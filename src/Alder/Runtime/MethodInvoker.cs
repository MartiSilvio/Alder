using System.Runtime.ExceptionServices;
using Alder.Binding;
using Alder.Diagnostics;
using Alder.Interpretation;

namespace Alder.Runtime;

internal static class MethodInvoker
{
    internal static object? InvokeResolvedMethod(MethodInfo method, object? target, object?[] args, AlderContext context)
    {
        if (method.IsStatic)
        {
            var declaringType = method.DeclaringType ?? throw new InvalidOperationException("Resolved static method has no declaring type.");
            if (TypedDispatchHelper.TryInvokeStatic(context.Config, declaringType, method.Name, args, out var staticResult))
                return staticResult;

            return InvokeMethodCore(method, null, args);
        }

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMethodCall, method.Name);

        if (TypedDispatchHelper.TryInvokeInstance(context.Config, target.GetType(), method.Name, target, args, out var typedResult))
            return typedResult;

        return InvokeMethodCore(method, target, args);
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
        if (target is Type staticType && !HasSpecialArgs(args))
        {
            var resolvedArgs = args.Length >= 2 && args[0] != null
                ? TryResolveLambdaArgs(args, args[0].GetType(), context) ?? args
                : args;
            if (TypedDispatchHelper.TryInvokeStatic(context.Config, staticType, methodName, resolvedArgs, out var staticResult))
                return staticResult;
        }

        var result = TryInvokeInstanceMethod(target, methodName, args, context, typeArgs, ct);
        if (result.Success)
            return result.Value;

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
                funcRef.Invoke(args),

            LambdaValue lambda =>
                InvokeLambda(lambda, args, context),

            CompiledLambdaValue compiled =>
                InvokeCompiledLambda(compiled, args),

            Delegate del =>
                TypeHelpers.GuardReflectionLeak(del.DynamicInvoke(args), "delegate invocation"),

            StaticMethodRef staticRef =>
                InvokeStaticMethod(staticRef.Type, staticRef.MethodName, args, context, typeArgs, ct),

            MethodRef methodRef =>
                InvokeMethodRef(methodRef, args, context, typeArgs, ct),

            null => throw new AlderException(DiagnosticDescriptors.NullInvocation),
            _ => throw new AlderException(DiagnosticDescriptors.NonCallableType, callee.GetType().Name)
        };
    }

    private static object? InvokeMethodRef(
        MethodRef methodRef,
        object?[] args,
        AlderContext context,
        IReadOnlyList<string>? typeArgs,
        CancellationToken ct)
    {
        var target = methodRef.Target;

        if (target is Range or InclusiveRange)
            target = Extensions.RangeHelpers.EnsureEnumerable(target);

        var result = TryInvokeInstanceMethod(
            target, methodRef.MethodName, args,
            context, typeArgs, ct);
        if (result.Success)
            return result.Value;

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
        var hasSpecialArgs = HasSpecialArgs(args);

        // Tier 1: typed dispatch (primary path for both JIT and AOT)
        if (!hasSpecialArgs &&
            TypedDispatchHelper.TryInvokeInstance(context.Config, type, methodName, target, args, out var typedResult))
            return (true, typedResult);

        // Tier 2: reflection with overload resolution and caching
        var descriptors = ArgumentDescriptor.FromArgs(args);

        if (typeArgs is null or { Count: 0 } &&
            ResolutionCache.TryGet(type, methodName, descriptors, out var cached))
        {
            return InvokeWithResolved(cached, target, args, ct);
        }

        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!context.Config.IsCaseSensitive)
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

        // Tier 3: extension methods, per-type interleaved dispatch (typed then reflection)
        var extensionResult = TryInvokeExtensionMethod(
            target, methodName, args, hasSpecialArgs, context, typeArgs, ct);
        if (extensionResult.Success)
            return extensionResult;

        return (false, null);
    }

    private static (bool Success, object? Value) TryInvokeExtensionMethod(
        object target, string methodName, object?[] args, bool hasSpecialArgs,
        AlderContext context,
        IReadOnlyList<string>? typeArgs, CancellationToken ct)
    {
        var extensionTypes = context.ExtensionTypes;
        if (extensionTypes.IsDefaultOrEmpty)
            return (false, null);

        var extArgs = PrependTarget(target, args);
        var resolvedArgs = TryResolveLambdaArgs(extArgs, target.GetType(), context) ?? extArgs;
        var resolvedInnerArgs = resolvedArgs == extArgs ? args : resolvedArgs.AsSpan(1).ToArray();

        // Try AOT extension dispatches first (value-type LINQ)
        if (!hasSpecialArgs && context.Config.ExtensionDispatches is { } extDispatches)
        {
            foreach (var dispatch in extDispatches)
            {
                if (dispatch.TryInvokeStatic(methodName, resolvedArgs, out var aotResult))
                    return (true, aotResult);
            }
        }

        foreach (var extType in extensionTypes)
        {
            if (!hasSpecialArgs && context.Config.TryGetDispatch(extType, out var extDispatch))
            {
                if (extDispatch.TryInvokeStatic(methodName, resolvedArgs, out var typedResult))
                    return (true, typedResult);
            }

            var reflResult = ExtensionMethodResolver.TryInvokeFromType(
                target, target.GetType(), methodName, resolvedInnerArgs, extType,
                context.Config.IsCaseSensitive, typeArgs, context, ct);
            if (reflResult.Success)
                return reflResult;
        }

        return (false, null);
    }

    private static object?[]? TryResolveLambdaArgs(object?[] extArgs, Type targetType, AlderContext context)
    {
        var elementType = TypeHelpers.GetEnumerableElementType(targetType);
        if (elementType == null)
            return null;

        var hasConvertible = false;
        for (var i = 1; i < extArgs.Length; i++)
        {
            if (extArgs[i] is LambdaValue or CompiledLambdaValue or StaticMethodRef or MethodRef or ModuleMethodRef)
            {
                hasConvertible = true;
                break;
            }
        }
        if (!hasConvertible)
            return null;

        var resolved = new object?[extArgs.Length];
        resolved[0] = extArgs[0];

        var inputTypes = new Binding.BoundType[] { new(elementType) };
        for (var i = 1; i < extArgs.Length; i++)
        {
            var arg = extArgs[i];

            if (arg is not (LambdaValue or CompiledLambdaValue or StaticMethodRef or MethodRef or ModuleMethodRef))
            {
                resolved[i] = arg;
                continue;
            }

            var returnType = ExtensionMethodResolver.InferLambdaReturnType(arg, inputTypes, context);
            if (returnType == null || returnType == typeof(object))
            {
                resolved[i] = arg;
                continue;
            }

            Delegate? converted = null;
            try
            {
                var delegateType = typeof(Func<,>).MakeGenericType(elementType, returnType);
                converted = LambdaDelegateConverter.TryConvert(arg!, delegateType);
            }
            catch
            {
                // MakeGenericType can fail on NativeAOT for rare type combinations.
                // The extension dispatch or reflection fallback handles these cases.
            }
            resolved[i] = converted ?? arg;
        }

        return resolved;
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

        if (methods.Length > 0)
            throw ClassifyOverloadFailure(methodName, methods, args, descriptors);

        throw new AlderException(DiagnosticDescriptors.MemberNotFound, module.Type.Name, methodName);
    }

    private static object? InvokeStaticMethod(
        Type type,
        string methodName,
        object?[] args,
        AlderContext context,
        IReadOnlyList<string>? typeArgs,
        CancellationToken ct)
    {
        if (!HasSpecialArgs(args) &&
            TypedDispatchHelper.TryInvokeStatic(context.Config, type, methodName, args, out var aotResult))
            return aotResult;

        var descriptors = ArgumentDescriptor.FromArgs(args);

        if (typeArgs is null or { Count: 0 } &&
            ResolutionCache.TryGet(type, methodName, descriptors, out var cached))
        {
            return InvokeWithResolved(cached, null, args, ct).Value;
        }

        var bindingFlags = BindingFlags.Public | BindingFlags.Static;
        if (!context.Config.IsCaseSensitive)
            bindingFlags |= BindingFlags.IgnoreCase;

        var methods = context.TypeMetadata.GetMethods(type, methodName, bindingFlags);

        if (OverloadResolver.TryResolve(methods, descriptors, context, out var resolved, out var ambiguous, typeArgs, ct))
        {
            ResolutionCache.Set(type, methodName, descriptors, resolved);
            return InvokeWithResolved(resolved, null, args, ct).Value;
        }

        if (ambiguous)
            throw new AlderException(DiagnosticDescriptors.AmbiguousMethodInvocation, $"{type.Name}.{methodName}");

        if (methods.Length > 0)
            throw ClassifyOverloadFailure(methodName, methods, args, descriptors);

        throw new AlderException(DiagnosticDescriptors.MemberNotFound, type.Name, methodName);
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
        return RuntimeGenericFactory.TryCloseGenericMethod(genericMethod, typeArgs, out var closed)
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
            foreach (var iface in receiverType.GetInterfaces())
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
