using System.Runtime.CompilerServices;
using Alder.Diagnostics;
using Alder.Interpretation;

namespace Alder.Runtime;

internal static class MethodInvoker
{
    public static object? InvokeMemberCall(
        object? target,
        string methodName,
        object?[] args,
        bool nullSafe,
        AlderContext context,
        AlderConfig config,
        IReadOnlyList<string>? typeArgs = null,
        CancellationToken ct = default)
    {
        if (nullSafe && target == null)
            return null;

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMethodCall, methodName);

        // When target is a Type (e.g., Enumerable.Range, string.Format), try typed static
        // dispatch first. On NativeAOT, reflection-based static method discovery is trimmed,
        // but the generated TryInvokeStatic handles these calls.
        if (target is Type staticType && !HasSpecialArgs(args) &&
            TypedDispatchHelper.TryInvokeStatic(config, staticType, methodName, args, out var staticResult))
            return staticResult;

        var result = TryInvokeInstanceMethod(target, methodName, args, context, config, typeArgs, ct);
        if (result.Success)
            return result.Value;

        var callee = MemberAccess.GetMember(target, methodName, config, nullSafe, context);
        return InvokeCall(callee, args, context, config, typeArgs, ct);
    }

    public static object? InvokeCall(
        object? callee,
        object?[] args,
        AlderContext context,
        AlderConfig config,
        IReadOnlyList<string>? typeArgs = null,
        CancellationToken ct = default)
    {
        return callee switch
        {
            ModuleMethodRef moduleRef =>
                InvokeModuleMethod(moduleRef, args, context, ct),

            FunctionRef funcRef =>
                funcRef.Invoke(args),

            LambdaValue lambda =>
                InvokeLambda(lambda, args, context),

            CompiledLambdaValue compiled =>
                InvokeCompiledLambda(compiled, args),

            Delegate del =>
                TypeHelpers.GuardReflectionLeak(del.DynamicInvoke(args), "delegate invocation"),

            StaticMethodRef staticRef =>
                InvokeStaticMethod(staticRef.Type, staticRef.MethodName, args, context, config, typeArgs, ct),

            MethodRef methodRef =>
                InvokeMethodRef(methodRef, args, context, config, typeArgs, ct),

            null => throw new AlderException(DiagnosticDescriptors.NullInvocation),
            _ => throw new AlderException(DiagnosticDescriptors.NonCallableType, callee.GetType().Name)
        };
    }

    private static object? InvokeMethodRef(
        MethodRef methodRef,
        object?[] args,
        AlderContext context,
        AlderConfig config,
        IReadOnlyList<string>? typeArgs,
        CancellationToken ct)
    {
        var target = methodRef.Target;

        if (target is Range or InclusiveRange)
            target = Extensions.RangeHelpers.EnsureEnumerable(target);

        var result = TryInvokeInstanceMethod(
            target, methodRef.MethodName, args,
            context, config, typeArgs, ct);
        if (result.Success)
            return result.Value;
        throw new AlderException(DiagnosticDescriptors.MethodInvocationFailed, methodRef.MethodName);
    }

    public static (bool Success, object? Value) TryInvokeInstanceMethod(
        object? target,
        string methodName,
        object?[] args,
        AlderContext context,
        AlderConfig config,
        IReadOnlyList<string>? typeArgs = null,
        CancellationToken ct = default)
    {
        if (target is null or ModuleInfo)
            return (false, null);

        var type = target.GetType();
        var hasSpecialArgs = HasSpecialArgs(args);

        // Tier 1: typed dispatch — primary path for both JIT and AOT
        if (!hasSpecialArgs &&
            TypedDispatchHelper.TryInvokeInstance(config, type, methodName, target, args, out var typedResult))
            return (true, typedResult);

        // Tier 2: reflection — overload resolution with caching
        var descriptors = ArgumentDescriptor.FromArgs(args);

        if (typeArgs is null or { Count: 0 } &&
            ResolutionCache.TryGet(type, methodName, descriptors, out var cached))
        {
            return InvokeWithResolved(cached, target, args, ct);
        }

        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!config.IsCaseSensitive)
            flags |= BindingFlags.IgnoreCase;
        var methods = context.TypeMetadata.GetMethods(type, methodName, flags);

        var argsWithCt = TryAppendCancellationTokenDescriptor(methods, descriptors, ct);

        if (OverloadResolver.TryResolve(methods, argsWithCt, context, out var resolved, out var ambiguous, typeArgs, ct))
        {
            ResolutionCache.Set(type, methodName, descriptors, resolved);
            return InvokeWithResolved(resolved, target, args, ct);
        }

        if (ambiguous)
            throw new AlderException(DiagnosticDescriptors.AmbiguousMethodInvocation, methodName);

        // Tier 3: extension methods — per-type interleaved dispatch (typed → reflection)
        var extensionResult = TryInvokeExtensionMethod(
            target, methodName, args, hasSpecialArgs, context, config, typeArgs, ct);
        if (extensionResult.Success)
            return extensionResult;

        return (false, null);
    }

    private static (bool Success, object? Value) TryInvokeExtensionMethod(
        object target, string methodName, object?[] args, bool hasSpecialArgs,
        AlderContext context, AlderConfig config,
        IReadOnlyList<string>? typeArgs, CancellationToken ct)
    {
        var extensionTypes = context.ExtensionTypes;
        if (extensionTypes.IsDefaultOrEmpty)
            return (false, null);

        object?[]? extArgs = null;

        foreach (var extType in extensionTypes)
        {
            // Try typed dispatch first for this extension type
            if (!hasSpecialArgs && config.TryGetDispatch(extType, out var extDispatch))
            {
                extArgs ??= PrependTarget(target, args);
                if (extDispatch.TryInvokeStatic(methodName, extArgs, out var typedResult))
                    return (true, typedResult);
            }

            // Typed dispatch didn't match — try reflection for this specific type
            var reflResult = ExtensionMethodResolver.TryInvokeFromType(
                target, target.GetType(), methodName, args, extType,
                config.IsCaseSensitive, typeArgs, context, ct);
            if (reflResult.Success)
                return reflResult;
        }

        return (false, null);
    }

    private static object?[] PrependTarget(object target, object?[] args)
    {
        var extArgs = new object?[args.Length + 1];
        extArgs[0] = target;
        Array.Copy(args, 0, extArgs, 1, args.Length);
        return extArgs;
    }

    internal static MethodInfo? TryMakeConcreteMethodWithTypeArgs(MethodInfo genericMethod, IReadOnlyList<string> typeArgs, TypeResolver? resolver = null)
    {
        var genericParams = genericMethod.GetGenericArguments();
        if (genericParams.Length != typeArgs.Count)
            return null;

        try
        {
            var resolvedTypes = new Type[typeArgs.Count];
            for (var i = 0; i < typeArgs.Count; i++)
            {
                Type? type;
                if (resolver != null)
                    type = resolver.TryResolveType(typeArgs[i]);
                else
                    return null;
                if (type == null)
                    return null;
                resolvedTypes[i] = type;
            }
            if (RuntimeGenericFactory.TryCloseGenericMethod(genericMethod, resolvedTypes, out var closedMethod))
                return closedMethod;
            return null;
        }
        catch (Exception ex) when (ex is ArgumentException or TypeLoadException or InvalidOperationException)
        {
            return null;
        }
    }

    private static object? InvokeModuleMethod(
        ModuleMethodRef methodRef,
        object?[] args,
        AlderContext context,
        CancellationToken ct)
    {
        var methodName = methodRef.Method.Name;
        var module = methodRef.Module;
        var target = methodRef.Method.IsStatic ? null : module.Resolve(methodRef.ServiceProvider);

        var methods = context.TypeMetadata.GetMethods(module.Type, methodName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        var descriptors = ArgumentDescriptor.FromArgs(args);
        if (OverloadResolver.TryResolve(methods, descriptors, context, out var resolved, out var ambiguous, ct: ct))
            return InvokeWithResolved(resolved, target, args, ct).Value;

        if (ambiguous)
            throw new AlderException(DiagnosticDescriptors.AmbiguousMethodInvocation, methodName);

        throw new AlderException(DiagnosticDescriptors.MethodInvocationFailed, methodName);
    }

    private static object? InvokeStaticMethod(
        Type type,
        string methodName,
        object?[] args,
        AlderContext context,
        AlderConfig config,
        IReadOnlyList<string>? typeArgs,
        CancellationToken ct)
    {
        if (!HasSpecialArgs(args) &&
            TypedDispatchHelper.TryInvokeStatic(config, type, methodName, args, out var aotResult))
            return aotResult;

        var descriptors = ArgumentDescriptor.FromArgs(args);

        if (typeArgs is null or { Count: 0 } &&
            ResolutionCache.TryGet(type, methodName, descriptors, out var cached))
        {
            return InvokeWithResolved(cached, null, args, ct).Value;
        }

        var bindingFlags = BindingFlags.Public | BindingFlags.Static;
        if (!config.IsCaseSensitive)
            bindingFlags |= BindingFlags.IgnoreCase;

        var methods = context.TypeMetadata.GetMethods(type, methodName, bindingFlags);

        if (OverloadResolver.TryResolve(methods, descriptors, context, out var resolved, out var ambiguous, typeArgs, ct))
        {
            ResolutionCache.Set(type, methodName, descriptors, resolved);
            return InvokeWithResolved(resolved, null, args, ct).Value;
        }

        if (ambiguous)
            throw new AlderException(DiagnosticDescriptors.AmbiguousMethodInvocation, $"{type.Name}.{methodName}");

        throw new AlderException(DiagnosticDescriptors.MemberNotFound, type.Name, methodName);
    }

    private static (bool Success, object? Value) InvokeWithResolved(
        ResolvedCall resolved,
        object? target,
        object?[] args,
        CancellationToken ct)
    {
        var parameters = MethodDispatchCache.GetParameters(resolved.Method);
        var prepared = ArgumentPreparer.Prepare(resolved, args, parameters, ct);
        var result = InvokeMethodCore(resolved.Method, target, prepared);
        ArgumentPreparer.CopyBackOutArgs(args, prepared, parameters);
        return (true, result);
    }

    internal static object? InvokeMethodCore(
        MethodInfo method,
        object? target,
        object?[] args)
    {
        try
        {
            if (!MethodDispatchCache.TryInvokeFast(method, target, args, out var result))
                result = method.Invoke(target, args);

            return TypeHelpers.GuardReflectionLeak(result, $"method {method.Name}");
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    internal static MethodInfo? TryMakeConcreteMethod(MethodInfo genericMethod, Type?[] argTypes)
    {
        var typeArgs = TypeInference.Infer(genericMethod, argTypes, lambdaArgs: null, runtimeContext: null);
        if (typeArgs == null)
            return null;
        return RuntimeGenericFactory.TryCloseGenericMethod(genericMethod, typeArgs, out var closed)
            ? closed : null;
    }

    private static bool HasSpecialArgs(object?[] args)
    {
        foreach (var arg in args)
        {
            if (arg is NamedArg or OutArgMarker)
                return true;
        }
        return false;
    }

    private static ArgumentDescriptor[] TryAppendCancellationTokenDescriptor(
        MethodInfo[] methods,
        ArgumentDescriptor[] descriptors,
        CancellationToken ct)
    {
        foreach (var method in methods)
        {
            var parameters = MethodDispatchCache.GetParameters(method);
            if (parameters.Length > 0 &&
                parameters[^1].ParameterType == typeof(CancellationToken) &&
                descriptors.Length == parameters.Length - 1)
            {
                var extended = new ArgumentDescriptor[descriptors.Length + 1];
                Array.Copy(descriptors, extended, descriptors.Length);
                extended[^1] = ArgumentDescriptor.FromArgs([ct])[0];
                return extended;
            }
        }
        return descriptors;
    }

    internal static object? InvokeLambda(LambdaValue lambda, object?[] args, AlderContext context)
    {
        var childContext = lambda.Closure.CreateChild();
        for (var i = 0; i < lambda.Parameters.Count && i < args.Length; i++)
            childContext.Define(lambda.Parameters[i], args[i]);

        var bound = lambda.GetOrBindBody(childContext);
        var evaluator = new BoundEvaluator(childContext, lambda.Config);
        var result = evaluator.Evaluate(bound);
        return result is ControlFlowSignal signal ? signal.Value : result;
    }

    internal static object? InvokeCompiledLambda(CompiledLambdaValue lambda, object?[] args)
    {
        return lambda.CompiledBody(args, lambda.Closure);
    }

    internal static object? InvokeCompiledLambda0(CompiledLambdaValue lambda)
    {
        if (lambda.CompiledBody0 != null)
            return lambda.CompiledBody0(lambda.Closure);
        return lambda.CompiledBody([], lambda.Closure);
    }

    internal static object? InvokeCompiledLambda1(CompiledLambdaValue lambda, object? arg0)
    {
        if (lambda.CompiledBody1 != null)
            return lambda.CompiledBody1(arg0, lambda.Closure);
        return lambda.CompiledBody([arg0], lambda.Closure);
    }

    internal static object? InvokeCompiledLambda2(CompiledLambdaValue lambda, object? arg0, object? arg1)
    {
        if (lambda.CompiledBody2 != null)
            return lambda.CompiledBody2(arg0, arg1, lambda.Closure);
        return lambda.CompiledBody([arg0, arg1], lambda.Closure);
    }
}
