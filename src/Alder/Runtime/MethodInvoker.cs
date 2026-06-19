using System.Runtime.ExceptionServices;
using System.Diagnostics.CodeAnalysis;
using Alder.Binding;
using Alder.Diagnostics;
using Alder.Interpretation;
using Alder.Runtime.OverloadResolution;

namespace Alder.Runtime;

internal static class MethodInvoker
{
    internal static object? InvokePreparedMethod(MethodInfo method, object? target, object?[] args, AlderContext context)
    {
        if (method.IsStatic)
        {
            var declaringType = method.DeclaringType ?? throw new InvalidOperationException("Resolved static method has no declaring type.");
            if (!MethodDispatchCache.DynamicCodeSupported)
            {
                if (TryInvokeStaticDispatch(
                        declaringType,
                        method.Name,
                        args,
                        context,
                        typeArgs: null,
                        method,
                        out var staticResult))
                    return staticResult;

                throw new AlderException(DiagnosticDescriptors.GeneratedMethodRequired, declaringType.Name, method.Name);
            }

            return InvokeMethodCore(method, null, args);
        }

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMethodCall, method.Name);

        if (!MethodDispatchCache.DynamicCodeSupported)
        {
            if (TypedDispatchHelper.TryInvokeInstance(context.Config, target.GetType(), method.Name, target, args, out var fallbackTypedResult))
                return fallbackTypedResult;

            throw new AlderException(DiagnosticDescriptors.GeneratedMethodRequired, target.GetType().Name, method.Name);
        }

        return InvokeMethodCore(method, target, args);
    }

    internal static object? InvokeResolvedCall(
        ResolvedCall resolved,
        object? target,
        object?[] args,
        AlderContext context,
        CancellationToken ct)
    {
        var parameters = MethodDispatchCache.GetParameters(resolved.Method);
        var prepared = ArgumentPreparer.Prepare(resolved, args, parameters, ct);

        try
        {
            if (!resolved.Method.IsStatic &&
                resolved.Method.DeclaringType is { IsGenericType: true } declaringType &&
                declaringType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return TypeHelpers.InvokeNullableInstanceMethod(declaringType, target, resolved.Method.Name, prepared);
            }

            return InvokePreparedMethod(resolved.Method, target, prepared, context);
        }
        finally
        {
            ArgumentPreparer.CopyBackOutArgs(args, prepared, parameters);
        }
    }

    public static object? InvokeMemberCall(
        object? target,
        string methodName,
        object?[] args,
        bool nullSafe,
        AlderContext context,
        IReadOnlyList<string>? typeArgs = null,
        CancellationToken ct = default)
    {
        if (nullSafe && target == null)
            return null;

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMethodCall, methodName);

        // Dynamic entry: the binder couldn't resolve this call at compile time. If the runtime
        // target is a `Type`, try the source-generated static dispatch as a fast path — that is
        // AOT-safe and avoids surfacing NativeAOT's internal `NativeFormatRuntimeNamedTypeInfo`
        // via reflection on `System.Type`. On a miss, fall through to instance dispatch on the
        // `Type` object itself so `typeof(int).ToString()` and `typeof(int).GetInterfaces()`
        // still resolve (the bound path handles these statically after the binder split, but
        // the fallback keeps genuinely dynamic callers working).
        if (target is Type staticType)
        {
            if (TryInvokeStaticDispatch(staticType, methodName, args, context, typeArgs, resolvedMethod: null, out var staticResult))
                return staticResult;

            if (!MethodDispatchCache.DynamicCodeSupported)
                throw new AlderException(DiagnosticDescriptors.GeneratedMethodRequired, staticType.Name, methodName);
        }

        var result = TryInvokeInstanceMethod(target, methodName, args, context, typeArgs, ct);
        if (result.Success)
            return result.Value;

        if (!MethodDispatchCache.DynamicCodeSupported)
            throw new AlderException(DiagnosticDescriptors.GeneratedMethodRequired, target.GetType().Name, methodName);

        var callee = MemberAccess.GetMember(target, methodName, nullSafe, context);
        return InvokeCall(callee, args, context, typeArgs, ct);
    }

    public static object? InvokeCall(
        object? callee,
        object?[] args,
        AlderContext context,
        IReadOnlyList<string>? typeArgs = null,
        CancellationToken ct = default)
    {
        return callee switch
        {
            ModuleMethodRef moduleRef =>
                InvokeModuleMethod(moduleRef, args, context, ct),

            FunctionRef funcRef =>
                funcRef.Function(args),

            LambdaValue lambda =>
                InvokeLambda(lambda, args, context),

            CompiledLambdaValue compiled =>
                InvokeCompiledLambda(compiled, args),

            Delegate del =>
                InvokeDelegate(del, args),

            MethodRef methodRef =>
                InvokeMethodRef(methodRef, args, context, typeArgs, ct),

            null => throw new AlderException(DiagnosticDescriptors.NullInvocation),
            _ => throw new AlderException(DiagnosticDescriptors.NonCallableType, callee.GetType().Name)
        };
    }

    internal static bool IsCallable(object? callee) => callee is
        FunctionRef or
        LambdaValue or
        CompiledLambdaValue or
        Delegate or
        ModuleMethodRef or
        MethodRef;

    private static object? InvokeDelegate(Delegate del, object?[] args)
    {
        try
        {
            return TypeHelpers.GuardReflectionLeak(del.DynamicInvoke(args), "delegate invocation");
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // unreachable, satisfies compiler
        }
    }

    private static object? InvokeMethodRef(
        MethodRef methodRef,
        object?[] args,
        AlderContext context,
        IReadOnlyList<string>? typeArgs,
        CancellationToken ct)
    {
        if (methodRef.Target is Type staticType)
            return InvokeStaticMethod(staticType, methodRef.MethodName, args, context, typeArgs, ct);

        var target = methodRef.Target;

        if (target is Range or InclusiveRange)
            target = Extensions.RangeHelpers.EnsureEnumerable(target);

        var result = TryInvokeInstanceMethod(
            target, methodRef.MethodName, args,
            context, typeArgs, ct);
        if (result.Success)
            return result.Value;

        if (!MethodDispatchCache.DynamicCodeSupported)
            throw new AlderException(DiagnosticDescriptors.GeneratedMethodRequired, target!.GetType().Name, methodRef.MethodName);

        if (!HasAnyMethodWithName(target!, methodRef.MethodName, context))
            throw new AlderException(DiagnosticDescriptors.MemberNotFound, target!.GetType().Name, methodRef.MethodName);

        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!context.Config.IsCaseSensitive) flags |= BindingFlags.IgnoreCase;
        var methods = context.TypeMetadata.GetMethods(target!.GetType(), methodRef.MethodName, flags);
        var descriptors = ArgumentDescriptor.FromArgs(args);
        throw ClassifyOverloadFailure(methodRef.MethodName, methods, args, descriptors);
    }

    public static (bool Success, object? Value) TryInvokeInstanceMethod(
        object? target,
        string methodName,
        object?[] args,
        AlderContext context,
        IReadOnlyList<string>? typeArgs = null,
        CancellationToken ct = default)
    {
        if (target is null or ModuleInfo)
            return (false, null);

        var type = target.GetType();
        var hasNamedArgs = HasNamedArgs(args);

        // Tier 1: typed dispatch (primary path for both JIT and AOT)
        if (!hasNamedArgs &&
            TypedDispatchHelper.TryInvokeInstance(context.Config, type, methodName, target, args, out var typedResult))
            return (true, typedResult);

        if (!MethodDispatchCache.DynamicCodeSupported)
            return (false, null);

        // Tier 2: reflection with overload resolution and caching
        var descriptors = ArgumentDescriptor.FromArgs(args);

        if (typeArgs is null or { Count: 0 } &&
            ResolutionCache.TryGet(type, methodName, descriptors, out var cached))
        {
            return (true, InvokeResolvedCall(cached, target, args, context, ct));
        }

        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!context.Config.IsCaseSensitive)
            flags |= BindingFlags.IgnoreCase;
        var methods = context.TypeMetadata.GetMethods(type, methodName, flags);

        var argsWithCt = TryAppendCancellationTokenDescriptor(methods, descriptors, ct);

        if (OverloadResolver.TryResolve(methods, argsWithCt, context, out var resolved, out var ambiguous, typeArgs, ct))
        {
            ResolutionCache.Set(type, methodName, descriptors, resolved);
            return (true, InvokeResolvedCall(resolved, target, args, context, ct));
        }

        if (ambiguous)
            throw new AlderException(DiagnosticDescriptors.AmbiguousMethodInvocation, methodName);

        // Tier 3: extension methods, per-type interleaved dispatch (typed then reflection)
        var extensionResult = ExtensionMethodResolver.TryInvoke(target, methodName, args, context, typeArgs, ct);
        if (extensionResult.Success)
            return extensionResult;

        return (false, null);
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
            if (RuntimeGenericClosure.TryCloseMethod(genericMethod, resolvedTypes, out var closedMethod))
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
        var methodName = methodRef.MethodName;
        var module = methodRef.Module;
        if (!module.Members.TryGetValue(methodName, out var memberEntry) || !memberEntry.HasMethods)
            throw new AlderException(DiagnosticDescriptors.MemberNotFound, module.Type.Name, methodName);

        var methods = memberEntry.Methods as MethodInfo[] ?? memberEntry.Methods.ToArray();
        return InvokeMethodGroup(
            methodName,
            methods,
            args,
            context,
            typeArgs: null,
            ct,
            targetFactory: static targetState =>
            {
                var moduleRef = (ModuleMethodRef)targetState!;
                return moduleRef.Module.Resolve(moduleRef.ServiceProvider);
            },
            targetState: methodRef,
            targetName: module.Type.Name);
    }

    private static object? InvokeStaticMethod(
        Type type,
        string methodName,
        object?[] args,
        AlderContext context,
        IReadOnlyList<string>? typeArgs,
        CancellationToken ct)
    {
        if (TryInvokeStaticDispatch(type, methodName, args, context, typeArgs, resolvedMethod: null, out var staticResult))
            return staticResult;

        if (!MethodDispatchCache.DynamicCodeSupported)
            throw new AlderException(DiagnosticDescriptors.GeneratedMethodRequired, type.Name, methodName);

        var bindingFlags = BindingFlags.Public | BindingFlags.Static;
        if (!context.Config.IsCaseSensitive)
            bindingFlags |= BindingFlags.IgnoreCase;

        var methods = context.TypeMetadata.GetMethods(type, methodName, bindingFlags);
        return InvokeMethodGroup(
            methodName,
            methods,
            args,
            context,
            typeArgs,
            ct,
            targetFactory: null,
            targetState: null,
            targetName: type.Name,
            cacheOwner: type);
    }

    private static AlderException ClassifyOverloadFailure(
        string methodName,
        MethodBase[] methods,
        object?[] args,
        ArgumentDescriptor[] descriptors)
    {
        // §12.6.4: classify overload resolution failures against the actual provided arguments so
        // callers see the same diagnostic codes Roslyn emits (CS1501/CS1503/CS1739), not the generic
        // CS7036 "missing required parameter" catch-all.
        foreach (var descriptor in descriptors)
        {
            if (descriptor.Name is null) continue;
            var matched = false;
            foreach (var method in methods)
            {
                foreach (var parameter in method.GetParameters())
                {
                    if (string.Equals(parameter.Name, descriptor.Name, StringComparison.Ordinal))
                    {
                        matched = true;
                        break;
                    }
                }
                if (matched) break;
            }
            if (!matched)
                return new AlderException(DiagnosticDescriptors.NoParameterNamed, methodName, descriptor.Name);
        }

        var argCount = args.Length;
        var anyCountMatch = false;
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var required = 0;
            foreach (var p in parameters)
                if (!p.HasDefaultValue) required++;
            if (argCount >= required && argCount <= parameters.Length)
            {
                anyCountMatch = true;
                break;
            }
        }

        if (!anyCountMatch)
            return new AlderException(DiagnosticDescriptors.NoOverloadTakesArguments, methodName, argCount.ToString());

        // Count matched at least one overload — the failure is an argument type mismatch.
        // Find the first argument position that does not satisfy any applicable overload.
        for (var i = 0; i < descriptors.Length; i++)
        {
            var descriptor = descriptors[i];
            if (descriptor.StaticType is null && descriptor.Kind != ArgumentKind.Null) continue;
            var compatible = false;
            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (i >= parameters.Length) continue;
                var expected = parameters[i].ParameterType;
                if (descriptor.Kind == ArgumentKind.Null)
                {
                    if (!expected.IsValueType || Nullable.GetUnderlyingType(expected) != null)
                    {
                        compatible = true;
                        break;
                    }
                }
                else if (descriptor.StaticType is not null
                    && TypeHelpers.CanImplicitlyConvert(descriptor.StaticType, expected))
                {
                    compatible = true;
                    break;
                }
            }
            if (!compatible)
            {
                var fromName = descriptor.Kind == ArgumentKind.Null ? "<null>" : descriptor.StaticType?.Name ?? "?";
                var toName = methods[0].GetParameters() is { Length: > 0 } firstParams && i < firstParams.Length
                    ? firstParams[i].ParameterType.Name
                    : "?";
                return new AlderException(DiagnosticDescriptors.ArgumentConversionFailed,
                    (i + 1).ToString(), fromName, toName);
            }
        }

        return new AlderException(DiagnosticDescriptors.MissingRequiredArgument, argCount.ToString(), methodName);
    }

    private static object? InvokeMethodGroup(
        string methodName,
        MethodInfo[] methods,
        object?[] args,
        AlderContext context,
        IReadOnlyList<string>? typeArgs,
        CancellationToken ct,
        Func<object?, object?>? targetFactory,
        object? targetState,
        string targetName,
        Type? cacheOwner = null)
    {
        var descriptors = ArgumentDescriptor.FromArgs(args);

        if (cacheOwner != null &&
            typeArgs is null or { Count: 0 } &&
            ResolutionCache.TryGet(cacheOwner, methodName, descriptors, out var cached))
        {
            var cachedTarget = cached.Method.IsStatic ? null : targetFactory?.Invoke(targetState);
            return InvokeResolvedCall(cached, cachedTarget, args, context, ct);
        }

        if (OverloadResolver.TryResolve(methods, descriptors, context, out var resolved, out var ambiguous, typeArgs, ct))
        {
            if (cacheOwner != null)
                ResolutionCache.Set(cacheOwner, methodName, descriptors, resolved);

            var resolvedTarget = resolved.Method.IsStatic ? null : targetFactory?.Invoke(targetState);
            return InvokeResolvedCall(resolved, resolvedTarget, args, context, ct);
        }

        if (ambiguous)
            throw new AlderException(DiagnosticDescriptors.AmbiguousMethodInvocation, methodName);

        if (methods.Length > 0)
            throw ClassifyOverloadFailure(methodName, methods, args, descriptors);

        throw new AlderException(DiagnosticDescriptors.MemberNotFound, targetName, methodName);
    }

    private static bool TryInvokeStaticDispatch(
        Type declaringType,
        string methodName,
        object?[] args,
        AlderContext context,
        IReadOnlyList<string>? typeArgs,
        MethodInfo? resolvedMethod,
        out object? result)
    {
        result = null;
        if (HasNamedArgs(args))
            return false;

        if (GenericStaticDispatchHelper.TryInvoke(
                context.Config,
                declaringType,
                methodName,
                args,
                typeArgs,
                context.TypeResolver,
                resolvedMethod,
                out result))
            return true;

        return TypedDispatchHelper.TryInvokeStatic(context.Config, declaringType, methodName, args, out result);
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

        return TypeHelpers.GuardReflectionLeak(result, "method", method.Name);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // unreachable, satisfies compiler
        }
    }

    internal static MethodInfo? TryMakeConcreteMethod(MethodInfo genericMethod, Type?[] argTypes)
    {
        var typeArgs = TypeInference.Infer(genericMethod, argTypes, lambdaArgs: null, runtimeContext: null);
        if (typeArgs == null)
            return null;
        return RuntimeGenericClosure.TryCloseMethod(genericMethod, typeArgs, out var closed)
            ? closed : null;
    }

    private static bool HasAnyMethodWithName(object target, string name, AlderContext context)
    {
        var type = target.GetType();
        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!context.Config.IsCaseSensitive) flags |= BindingFlags.IgnoreCase;

        if (context.TypeMetadata.GetMethods(type, name, flags).Length > 0)
            return true;

        var extFlags = BindingFlags.Public | BindingFlags.Static;
        if (!context.Config.IsCaseSensitive) extFlags |= BindingFlags.IgnoreCase;

        // §12.8.9.3: an extension method is a *member of the receiver type* only when the
        // receiver is implicitly convertible to the extension's first parameter. Otherwise
        // the method name is not a member and the diagnostic must be CS1061, not CS1501.
        foreach (var extType in context.ExtensionTypes)
        {
            foreach (var m in context.TypeMetadata.GetMethods(extType, name, extFlags))
            {
                if (!m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                    continue;
                var parameters = m.GetParameters();
                if (parameters.Length == 0) continue;
                if (IsExtensionReceiverApplicable(type, parameters[0].ParameterType))
                    return true;
            }
        }

        return false;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2067",
        Justification = "Extension receiver discovery is a JIT/reflection fallback used only after generated-mode dispatch is unavailable; NativeAOT hosts must use registered/generated types whose interface metadata is rooted.")]
    private static bool IsExtensionReceiverApplicable(Type receiverType, Type firstParamType)
    {
        if (firstParamType.IsAssignableFrom(receiverType))
            return true;
        if (TypeHelpers.CanImplicitlyConvert(receiverType, firstParamType))
            return true;
        // Open-generic extensions (e.g. IEnumerable<T>) require inference — accept if the
        // receiver implements the generic type definition of the first parameter.
        if (firstParamType.IsGenericType)
        {
            var def = firstParamType.GetGenericTypeDefinition();
            if (receiverType.IsGenericType && receiverType.GetGenericTypeDefinition() == def)
                return true;
            foreach (var iface in RuntimeTypeIntrospection.GetInterfaces(receiverType))
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == def)
                    return true;
            }
            var baseType = receiverType.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == def)
                    return true;
                baseType = baseType.BaseType;
            }
        }
        return false;
    }

    private static bool HasNamedArgs(object?[] args)
    {
        foreach (var arg in args)
        {
            if (arg is NamedArg)
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
        lambda.Closure.GetActiveCancellationToken().ThrowIfCancellationRequested();

        if (lambda.IsAsync)
            return InvokeLambdaAsyncCore(lambda, args);

        if (lambda.IsIterator)
            return IteratorEnumerable.Create(lambda, args, context);

        var childContext = lambda.Closure.CreateChild();
        for (var i = 0; i < lambda.Parameters.Count && i < args.Length; i++)
            childContext.Define(lambda.Parameters[i], args[i]);

        var bound = lambda.GetOrBindBody(childContext);
        var evaluator = new BoundEvaluator(childContext);
        var result = evaluator.Evaluate(bound);
        return result is ControlFlowSignal signal ? signal.Value : result;
    }

    // §12.19: async lambda invocation returns Task<object?>, body is evaluated asynchronously
    private static Task<object?> InvokeLambdaAsyncCore(LambdaValue lambda, object?[] args)
    {
        var childContext = lambda.Closure.CreateChild();
        for (var i = 0; i < lambda.Parameters.Count && i < args.Length; i++)
            childContext.Define(lambda.Parameters[i], args[i]);

        var bound = lambda.GetOrBindBody(childContext);
        var evaluator = new BoundEvaluator(childContext);
        return EvaluateAsyncAndUnwrap(evaluator, bound);
    }

    private static async Task<object?> EvaluateAsyncAndUnwrap(BoundEvaluator evaluator, BoundExpr bound)
    {
        var result = await evaluator.EvaluateAsync(bound);
        return result is ControlFlowSignal signal ? signal.Value : result;
    }

    internal static object? InvokeCompiledLambda(CompiledLambdaValue lambda, object?[] args)
    {
        lambda.Closure.GetActiveCancellationToken().ThrowIfCancellationRequested();
        return lambda.CompiledBody(args, lambda.Closure);
    }

    internal static object? InvokeCompiledLambda0(CompiledLambdaValue lambda)
    {
        lambda.Closure.GetActiveCancellationToken().ThrowIfCancellationRequested();
        if (lambda.CompiledBody0 != null)
            return lambda.CompiledBody0(lambda.Closure);
        return lambda.CompiledBody([], lambda.Closure);
    }

    internal static object? InvokeCompiledLambda1(CompiledLambdaValue lambda, object? arg0)
    {
        lambda.Closure.GetActiveCancellationToken().ThrowIfCancellationRequested();
        if (lambda.CompiledBody1 != null)
            return lambda.CompiledBody1(arg0, lambda.Closure);
        return lambda.CompiledBody([arg0], lambda.Closure);
    }

    internal static object? InvokeCompiledLambda2(CompiledLambdaValue lambda, object? arg0, object? arg1)
    {
        lambda.Closure.GetActiveCancellationToken().ThrowIfCancellationRequested();
        if (lambda.CompiledBody2 != null)
            return lambda.CompiledBody2(arg0, arg1, lambda.Closure);
        return lambda.CompiledBody([arg0, arg1], lambda.Closure);
    }
}
