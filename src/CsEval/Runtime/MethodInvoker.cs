using CsEval.Diagnostics;
using CsEval.Interpretation;

namespace CsEval.Runtime;

/// <summary>
/// Method invocation and dispatch.
/// </summary>
internal static class MethodInvoker
{
    public static object? InvokeMemberCall(
        object? target,
        string methodName,
        object?[] args,
        bool nullSafe,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        IReadOnlyList<string>? typeArgs = null)
    {
        if (nullSafe && target == null)
            return null;

        if (target == null)
            throw new CsEvalException($"Cannot call method '{methodName}' on null");

        var result = TryInvokeInstanceMethod(target, methodName, args, context, options, ct, typeArgs);
        if (result.Success)
            return result.Value;

        var callee = MemberAccess.GetMember(target, methodName, options, nullSafe, context);
        return InvokeCall(callee, args, context, options, ct, typeArgs);
    }

    public static object? InvokeCall(
        object? callee,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        IReadOnlyList<string>? typeArgs = null)
    {
        return callee switch
        {
            // ── Tier 1: Always allowed ──────────────────────────────────────
            // Host-registered or expression-authored callees. The host explicitly
            // made these available, so sandbox restrictions do not apply.
            ModuleMethodRef moduleRef =>
                InvokeModuleMethod(moduleRef, args, context, ct),

            FunctionRef funcRef =>
                funcRef.Invoke(args),

            LambdaValue lambda =>
                InvokeLambda(lambda, args, context),

            CompiledLambdaValue compiled =>
                InvokeCompiledLambda(compiled, args),

            Delegate del =>
                del.DynamicInvoke(args),

            // ── Tier 2: Requires AllowMethodCalls ───────────────────────────
            // Instance and static method calls on user-provided or resolved types.
            // Gated by AllowMethodCalls.
            StaticMethodRef staticRef =>
                options.Sandbox.AllowMethodCalls
                    ? InvokeStaticMethod(staticRef.Type, staticRef.MethodName, args, context, options, ct, typeArgs)
                    : throw new CsEvalException($"Static method calls blocked by sandbox: {staticRef.Type.Name}.{staticRef.MethodName}"),

            MethodRef methodRef =>
                InvokeMethodRef(methodRef, args, context, options, ct, typeArgs),

            // ── Unrecognized ────────────────────────────────────────────────
            null => throw new CsEvalException("Cannot call null as a function"),
            _ => throw new CsEvalException($"Cannot call '{callee.GetType().Name}' as a function")
        };
    }

    private static object? InvokeMethodRef(
        MethodRef methodRef,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        IReadOnlyList<string>? typeArgs)
    {
        var result = TryInvokeInstanceMethod(
            methodRef.Target, methodRef.MethodName, args,
            context, options, ct, typeArgs);
        if (result.Success)
            return result.Value;
        throw new CsEvalException($"Method '{methodRef.MethodName}' invocation failed");
    }

    public static (bool Success, object? Value) TryInvokeInstanceMethod(
        object? target,
        string methodName,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        IReadOnlyList<string>? typeArgs = null)
    {
        if (target == null)
            return (false, null);

        if (target is ModuleInfo)
            return (false, null);

        // ECMA-334 §12.8.9.2: Instance methods take precedence over extension methods.
        // "An extension method is eligible if [...] normal processing of the invocation
        // found no applicable instance methods."
        // Try instance methods first, then fall back to extension methods.

        // Instance methods are blocked in sandbox mode
        if (options.Sandbox.AllowMethodCalls)
        {
            var type = target.GetType();
            var flags = BindingFlags.Public | BindingFlags.Instance;
            if (!options.IsCaseSensitive)
                flags |= BindingFlags.IgnoreCase;
            var methods = context.TypeCache.GetMethods(type, methodName, flags);

            if (typeArgs == null || typeArgs.Count == 0)
            {
                var canFastPath = true;
                var argTypes = new Type[args.Length];
                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] == null || args[i] is NamedArg || args[i] is OutArgMarker)
                    {
                        canFastPath = false;
                        break;
                    }
                    argTypes[i] = args[i]!.GetType();
                }

                if (canFastPath)
                {
                    var fastMethod = MethodResolver.TryResolveMethod(type, methodName, argTypes, flags);
                    if (fastMethod != null)
                    {
                        var invokeResult = InvokeMethodWithArgs(fastMethod, target, args, ct);
                        if (invokeResult.Success)
                            return invokeResult;
                    }
                }
            }

            var candidateMethods = new List<MethodInfo>();
            foreach (var method in methods)
            {
                var concreteMethod = method;

                // Handle explicit type arguments for generic methods
                if (method.ContainsGenericParameters && typeArgs is { Count: > 0 })
                {
                    concreteMethod = TryMakeConcreteMethodWithTypeArgs(method, typeArgs, context.TypeResolver);
                    if (concreteMethod == null)
                        continue;
                }
                candidateMethods.Add(concreteMethod);
            }

            var bestMethod = FindBestMethod(candidateMethods, args, ct, out var ambiguous);
            if (ambiguous)
                throw new CsEvalException($"Ambiguous method invocation: '{methodName}'");

            if (bestMethod != null)
            {
                var invokeResult = InvokeMethodWithArgs(bestMethod, target, args, ct);
                if (invokeResult.Success)
                    return invokeResult;
            }
        }

        // No applicable instance method found (or instance methods blocked).
        // Try extension methods per ECMA-334 §12.8.9.2.
        var extensionResult = ExtensionMethodResolver.TryInvokeExtensionMethod(
            target, methodName, args, context.ExtensionTypes, ct, options.IsCaseSensitive, typeArgs, context.TypeResolver);
        if (extensionResult.Success)
            return extensionResult;

        // If sandbox blocks method calls and no extension method matched, report the block
        if (!options.Sandbox.AllowMethodCalls)
            throw new CsEvalException($"Method calls blocked by sandbox: {methodName}");

        return (false, null);
    }

    /// <summary>
    /// Makes a generic method concrete using explicit type arguments.
    /// Uses TypeResolver when available, falls back to Type.GetType for IL-compiled code paths.
    /// </summary>
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
                    type = Type.GetType(typeArgs[i]) ?? Type.GetType($"System.{typeArgs[i]}");
                if (type == null)
                    return null;
                resolvedTypes[i] = type;
            }
            return genericMethod.MakeGenericMethod(resolvedTypes);
        }
        catch (Exception ex) when (ex is ArgumentException or TypeLoadException or InvalidOperationException)
        {
            return null;
        }
    }

    private static object? InvokeModuleMethod(
        ModuleMethodRef methodRef,
        object?[] args,
        CsEvalContext context,
        CancellationToken ct)
    {
        var methodName = methodRef.Method.Name;
        var module = methodRef.Module;
        var target = methodRef.Method.IsStatic ? null : module.Resolve(methodRef.ServiceProvider);

        var methods = context.TypeCache.GetMethods(module.Type, methodName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        var nonGenericMethods = methods.Where(m => !m.ContainsGenericParameters);
        var bestMethod = FindBestMethod(nonGenericMethods, args, ct, out var ambiguous);
        if (ambiguous)
            throw new CsEvalException($"Ambiguous method invocation: '{methodName}'");

        if (bestMethod != null)
        {
            var invokeResult = InvokeMethodWithArgs(bestMethod, target, args, ct);
            if (invokeResult.Success)
                return invokeResult.Value;
        }

        // Try generic methods
        foreach (var method in methods.Where(m => m.ContainsGenericParameters))
        {
            var concreteMethod = TryMakeConcreteMethod(method, args);
            if (concreteMethod != null)
            {
                var result = InvokeMethodWithArgs(concreteMethod, target, args, ct);
                if (result.Success)
                    return result.Value;
            }
        }

        // Fallback to the registered method
        var fallbackMethod = methodRef.Method;
        var fallbackParams = fallbackMethod.GetParameters();
        var finalArgs = TryAppendCancellationToken(fallbackParams, args, ct);

        finalArgs = PadWithDefaults(fallbackParams, finalArgs, fallbackMethod.Name);

        var fallbackResult = fallbackMethod.Invoke(target, finalArgs);
        return TypeHelpers.GuardReflectionLeak(fallbackResult, $"method {methodName}");
    }

    private static object? InvokeStaticMethod(
        Type type,
        string methodName,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        IReadOnlyList<string>? typeArgs)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.Static;
        if (!options.IsCaseSensitive)
            bindingFlags |= BindingFlags.IgnoreCase;

        var methods = context.TypeCache.GetMethods(type, methodName, bindingFlags);

        if (typeArgs == null || typeArgs.Count == 0)
        {
            var canFastPath = true;
            var argTypes = new Type[args.Length];
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == null || args[i] is NamedArg || args[i] is OutArgMarker)
                {
                    canFastPath = false;
                    break;
                }
                argTypes[i] = args[i]!.GetType();
            }

            if (canFastPath)
            {
                var fastMethod = MethodResolver.TryResolveMethod(type, methodName, argTypes, bindingFlags);
                if (fastMethod != null)
                {
                    var invokeResult = InvokeMethodWithArgs(fastMethod, null, args, ct);
                    if (invokeResult.Success)
                        return invokeResult.Value;
                }
            }
        }

        var nonGenericMethods = methods.Where(m => !m.ContainsGenericParameters);
        var bestMethod = FindBestMethod(nonGenericMethods, args, ct, out var ambiguous);
        if (ambiguous)
            throw new CsEvalException($"Ambiguous method invocation: '{type.Name}.{methodName}'");

        if (bestMethod != null)
        {
            var invokeResult = InvokeMethodWithArgs(bestMethod, null, args, ct);
            if (invokeResult.Success)
                return invokeResult.Value;
        }

        // Try generic methods with explicit type arguments first, then inference
        foreach (var method in methods.Where(m => m.ContainsGenericParameters))
        {
            MethodInfo? concreteMethod = null;

            // Try explicit type arguments first (e.g., Array.Empty<int>())
            if (typeArgs is { Count: > 0 })
            {
                concreteMethod = TryMakeConcreteMethodWithTypeArgs(method, typeArgs, context.TypeResolver);
            }

            // Fall back to inference from arguments
            concreteMethod ??= TryMakeConcreteMethod(method, args);

            if (concreteMethod != null)
            {
                var result = InvokeMethodWithArgs(concreteMethod, null, args, ct);
                if (result.Success)
                    return result.Value;
            }
        }

        throw new CsEvalException(DiagnosticDescriptors.MemberNotFound, type.Name, methodName);
    }

    internal static (bool Success, object? Value) InvokeMethodWithArgs(
        MethodInfo method,
        object? target,
        object?[] args,
        CancellationToken ct)
    {
        var parameters = method.GetParameters();
        var argsWithCancellation = TryAppendCancellationToken(parameters, args, ct);

        if (CanInvokeMethod(parameters, argsWithCancellation, out var convertedArgs))
        {
            try
            {
                var result = method.Invoke(target, convertedArgs);
                CopyBackOutArgs(args, convertedArgs, parameters);
                return (true, TypeHelpers.GuardReflectionLeak(result, $"method {method.Name}"));
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        return (false, null);
    }

    private static MethodInfo? TryMakeConcreteMethod(MethodInfo genericMethod, object?[] args)
    {
        var genericArgs = genericMethod.GetGenericArguments();

        if (genericArgs.Length != 1 || args.Length == 0)
            return null;

        var firstArg = args[0];
        if (firstArg == null)
            return null;

        var firstArgType = firstArg.GetType();

        // Try proper generic type inference from the first parameter's type structure
        // e.g., IEnumerable<TSource> + List<int> -> TSource = int
        var parameters = genericMethod.GetParameters();
        if (parameters.Length > 0)
        {
            var inferred = TryInferGenericArg(parameters[0].ParameterType, firstArgType, genericArgs[0]);
            if (inferred != null)
            {
                try { return genericMethod.MakeGenericMethod(inferred); }
                catch (Exception ex) when (ex is ArgumentException or TypeLoadException or InvalidOperationException) { }
            }
        }

        // Fallback: use the argument type directly
        try
        {
            return genericMethod.MakeGenericMethod(firstArgType);
        }
        catch (Exception ex) when (ex is ArgumentException or TypeLoadException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Infers a generic type argument by matching a parameter's generic type structure
    /// against the actual argument type. For example, IEnumerable&lt;TSource&gt; matched
    /// against List&lt;int&gt; infers TSource = int.
    /// </summary>
    private static Type? TryInferGenericArg(Type parameterType, Type argumentType, Type genericArg)
    {
        if (parameterType == genericArg)
            return argumentType;

        if (!parameterType.IsGenericType)
            return null;

        var genericDef = parameterType.GetGenericTypeDefinition();

        // Check if argumentType itself or its interfaces match the generic definition
        foreach (var candidate in argumentType.GetInterfaces().Prepend(argumentType))
        {
            if (!candidate.IsGenericType || candidate.GetGenericTypeDefinition() != genericDef)
                continue;

            var candidateArgs = candidate.GetGenericArguments();
            var paramArgs = parameterType.GetGenericArguments();

            for (var i = 0; i < paramArgs.Length; i++)
            {
                if (paramArgs[i] == genericArg)
                    return candidateArgs[i];
            }
        }

        return null;
    }

    private static object?[] TryAppendCancellationToken(ParameterInfo[] parameters, object?[] args, CancellationToken ct)
    {
        if (parameters.Length == 0)
            return args;

        var lastParam = parameters[^1];
        if (lastParam.ParameterType == typeof(CancellationToken) && args.Length == parameters.Length - 1)
        {
            var newArgs = new object?[args.Length + 1];
            Array.Copy(args, newArgs, args.Length);
            newArgs[^1] = ct;
            return newArgs;
        }

        return args;
    }

    private static bool CanInvokeMethod(ParameterInfo[] parameters, object?[] args, out object?[] convertedArgs)
    {
        convertedArgs = new object?[parameters.Length];

        var positionalArgs = new List<object?>();
        var namedArgs = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var arg in args)
        {
            if (arg is NamedArg named)
                namedArgs[named.Name] = named.Value;
            else
                positionalArgs.Add(arg);
        }

        var filledParams = new bool[parameters.Length];
        var positionalIndex = 0;

        for (var i = 0; i < parameters.Length && positionalIndex < positionalArgs.Count; i++)
        {
            if (namedArgs.ContainsKey(parameters[i].Name!))
                continue;

            var arg = positionalArgs[positionalIndex++];

            // OutArgMarker matches ByRef parameters (out/ref)
            if (arg is OutArgMarker && parameters[i].ParameterType.IsByRef)
            {
                var elementType = parameters[i].ParameterType.GetElementType()!;
                convertedArgs[i] = elementType.IsValueType ? Activator.CreateInstance(elementType) : null;
                filledParams[i] = true;
                continue;
            }

            if (!TryConvertArg(arg, parameters[i].ParameterType, out var converted))
                return false;

            convertedArgs[i] = converted;
            filledParams[i] = true;
        }

        // ECMA-334 §12.6.4.2: If too many positional args remain, try params expanded form.
        if (positionalIndex < positionalArgs.Count)
        {
            if (!TryPackParamsExpanded(parameters, positionalArgs, positionalIndex, convertedArgs, filledParams))
                return false;
        }

        foreach (var (name, value) in namedArgs)
        {
            var paramIndex = -1;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (string.Equals(parameters[i].Name, name, StringComparison.Ordinal))
                {
                    paramIndex = i;
                    break;
                }
            }

            if (paramIndex == -1)
                return false;

            if (!TryConvertArg(value, parameters[paramIndex].ParameterType, out var converted))
                return false;

            convertedArgs[paramIndex] = converted;
            filledParams[paramIndex] = true;
        }

        for (var i = 0; i < parameters.Length; i++)
        {
            if (filledParams[i])
                continue;

            if (parameters[i].HasDefaultValue)
            {
                convertedArgs[i] = parameters[i].DefaultValue;
            }
            else if (parameters[i].IsDefined(typeof(ParamArrayAttribute), false))
            {
                // ECMA-334 §12.6.4.2: Unfilled params parameter gets an empty typed array
                var elementType = parameters[i].ParameterType.GetElementType()!;
                convertedArgs[i] = Array.CreateInstance(elementType, 0);
                filledParams[i] = true;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Attempts to pack surplus positional arguments into a params array for the last parameter.
    /// ECMA-334 §12.6.4.2: expanded form of methods with params parameters.
    /// </summary>
    private static bool TryPackParamsExpanded(
        ParameterInfo[] parameters,
        List<object?> positionalArgs,
        int positionalIndex,
        object?[] convertedArgs,
        bool[] filledParams)
    {
        if (parameters.Length == 0)
            return false;

        var lastParam = parameters[^1];
        if (!lastParam.IsDefined(typeof(ParamArrayAttribute), false))
            return false;

        var elementType = lastParam.ParameterType.GetElementType()!;
        var lastParamIndex = parameters.Length - 1;

        var paramsArgCount = positionalArgs.Count - lastParamIndex;
        if (paramsArgCount < 0)
            return false;

        var paramsArray = Array.CreateInstance(elementType, paramsArgCount);
        for (var i = 0; i < paramsArgCount; i++)
        {
            var arg = positionalArgs[lastParamIndex + i];
            if (!TryConvertArg(arg, elementType, out var converted))
                return false;
            paramsArray.SetValue(converted, i);
        }

        convertedArgs[lastParamIndex] = paramsArray;
        filledParams[lastParamIndex] = true;
        return true;
    }

    private static bool TryConvertArg(object? arg, Type targetType, out object? converted)
    {
        converted = null;

        if (arg == null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                return false;
            return true;
        }

        var argType = arg.GetType();

        // Exact type match
        if (argType == targetType)
        {
            converted = arg;
            return true;
        }

        // Reference type assignability
        if (targetType.IsAssignableFrom(argType))
        {
            converted = arg;
            return true;
        }

        // Only allow implicit numeric conversions (no narrowing)
        if (TypeHelpers.CanImplicitlyConvert(argType, targetType))
        {
            converted = TypeHelpers.CoerceNumeric(arg, targetType);
            return true;
        }

        // Lambda to delegate conversion (e.g., LambdaValue -> Func<int>)
        if (arg is LambdaValue or CompiledLambdaValue)
        {
            var delegateResult = LambdaDelegateConverter.TryConvert(arg, targetType);
            if (delegateResult != null)
            {
                converted = delegateResult;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Scores how well arguments match a method's parameters per ECMA-334 §12.6.4.
    /// Tries normal form first, then expanded params form per §12.6.4.2.
    /// Higher score = better match. -1 = no match.
    /// </summary>
    private static int ScoreMethodMatch(ParameterInfo[] parameters, object?[] args)
    {
        var normalScore = ScoreMethodMatchNormalForm(parameters, args);
        if (normalScore >= 0)
            return normalScore;

        // ECMA-334 §12.6.4.2: If normal form fails, try expanded form for params methods.
        return ScoreMethodMatchExpandedForm(parameters, args);
    }

    /// <summary>
    /// Scores normal form: each arg maps 1:1 to a parameter. No params expansion.
    /// </summary>
    private static int ScoreMethodMatchNormalForm(ParameterInfo[] parameters, object?[] args)
    {
        var positionalArgs = new List<object?>();
        var namedArgs = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var arg in args)
        {
            if (arg is NamedArg named)
                namedArgs[named.Name] = named.Value;
            else
                positionalArgs.Add(arg);
        }

        var maxPositional = parameters.Length - namedArgs.Count;
        if (positionalArgs.Count > maxPositional)
            return -1;

        var score = 0;
        var defaultsUsed = 0;
        var filledParams = new bool[parameters.Length];
        var positionalIndex = 0;

        for (var i = 0; i < parameters.Length && positionalIndex < positionalArgs.Count; i++)
        {
            if (namedArgs.ContainsKey(parameters[i].Name!))
                continue;

            var arg = positionalArgs[positionalIndex++];
            var paramScore = ScoreArgument(arg, parameters[i].ParameterType);
            if (paramScore < 0)
                return -1;

            score += paramScore;
            filledParams[i] = true;
        }

        if (positionalIndex < positionalArgs.Count)
            return -1;

        foreach (var (name, value) in namedArgs)
        {
            var paramIndex = -1;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (string.Equals(parameters[i].Name, name, StringComparison.Ordinal))
                {
                    paramIndex = i;
                    break;
                }
            }

            if (paramIndex == -1)
                return -1;

            if (filledParams[paramIndex])
                return -1;

            var paramScore = ScoreArgument(value, parameters[paramIndex].ParameterType);
            if (paramScore < 0)
                return -1;

            score += paramScore;
            filledParams[paramIndex] = true;
        }

        for (var i = 0; i < parameters.Length; i++)
        {
            if (!filledParams[i] && !parameters[i].HasDefaultValue)
                return -1;

            if (!filledParams[i] && parameters[i].HasDefaultValue)
                defaultsUsed++;
        }

        const int applicableBaseScore = 1000;
        const int defaultArgumentPenalty = 10;
        return applicableBaseScore + score - (defaultsUsed * defaultArgumentPenalty);
    }

    /// <summary>
    /// Scores expanded params form per ECMA-334 §12.6.4.2:
    /// surplus positional args are packed into the params array.
    /// Returns a lower score than normal form to ensure normal form is preferred.
    /// </summary>
    private static int ScoreMethodMatchExpandedForm(ParameterInfo[] parameters, object?[] args)
    {
        if (parameters.Length == 0)
            return -1;

        var lastParam = parameters[^1];
        if (!lastParam.IsDefined(typeof(ParamArrayAttribute), false))
            return -1;

        var elementType = lastParam.ParameterType.GetElementType()!;
        var lastParamIndex = parameters.Length - 1;

        var positionalArgs = new List<object?>();
        var namedArgs = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var arg in args)
        {
            if (arg is NamedArg named)
                namedArgs[named.Name] = named.Value;
            else
                positionalArgs.Add(arg);
        }

        var score = 0;
        var defaultsUsed = 0;
        var filledParams = new bool[parameters.Length];
        var positionalIndex = 0;

        // Score non-params parameters normally
        for (var i = 0; i < lastParamIndex && positionalIndex < positionalArgs.Count; i++)
        {
            if (namedArgs.ContainsKey(parameters[i].Name!))
                continue;

            var arg = positionalArgs[positionalIndex++];
            var paramScore = ScoreArgument(arg, parameters[i].ParameterType);
            if (paramScore < 0)
                return -1;

            score += paramScore;
            filledParams[i] = true;
        }

        // Score remaining positional args against the params element type
        while (positionalIndex < positionalArgs.Count)
        {
            var arg = positionalArgs[positionalIndex++];
            var paramScore = ScoreArgument(arg, elementType);
            if (paramScore < 0)
                return -1;
            score += paramScore;
        }
        filledParams[lastParamIndex] = true;

        // Match named arguments (non-params parameters only)
        foreach (var (name, value) in namedArgs)
        {
            var paramIndex = -1;
            for (var i = 0; i < lastParamIndex; i++)
            {
                if (string.Equals(parameters[i].Name, name, StringComparison.Ordinal))
                {
                    paramIndex = i;
                    break;
                }
            }

            if (paramIndex == -1)
                return -1;

            if (filledParams[paramIndex])
                return -1;

            var paramScore = ScoreArgument(value, parameters[paramIndex].ParameterType);
            if (paramScore < 0)
                return -1;

            score += paramScore;
            filledParams[paramIndex] = true;
        }

        // Check unfilled non-params parameters have defaults
        for (var i = 0; i < lastParamIndex; i++)
        {
            if (!filledParams[i] && !parameters[i].HasDefaultValue)
                return -1;

            if (!filledParams[i] && parameters[i].HasDefaultValue)
                defaultsUsed++;
        }

        // ECMA-334 §12.6.4.3: Normal form is always preferred over expanded form.
        const int expandedBaseScore = 500;
        const int defaultArgumentPenalty = 10;
        return expandedBaseScore + score - (defaultsUsed * defaultArgumentPenalty);
    }

    /// <summary>
    /// Scores how well a single argument matches a parameter type.
    /// </summary>
    private static int ScoreArgument(object? arg, Type paramType)
    {
        if (arg == null)
        {
            if (paramType.IsValueType && Nullable.GetUnderlyingType(paramType) == null)
                return -1;
            return 1; // Null to nullable is valid but not exact
        }

        // OutArgMarker matches ByRef parameters (out/ref) as exact match
        if (arg is OutArgMarker && paramType.IsByRef)
            return 100;

        var argType = arg.GetType();

        if (argType == paramType)
            return 100; // Exact match - highest priority

        if (paramType.IsAssignableFrom(argType))
            return 10; // Assignable (base class, interface)

        if (TypeHelpers.CanImplicitlyConvert(argType, paramType))
            return 1; // Implicit conversion - lowest priority

        // Lambda to delegate (e.g., LambdaValue -> Func<int, bool>)
        if (arg is LambdaValue or CompiledLambdaValue && LambdaDelegateConverter.IsSupportedDelegateType(paramType))
        {
            try
            {
                return LambdaDelegateConverter.TryConvert(arg, paramType) != null ? 5 : -1;
            }
            catch (CsEvalException)
            {
                return -1;
            }
        }

        return -1; // No valid conversion
    }

    /// <summary>
    /// Finds the best matching method from candidates using overload resolution scoring.
    /// </summary>
    internal static MethodInfo? FindBestMethod(IEnumerable<MethodInfo> methods, object?[] args, CancellationToken ct, out bool ambiguous)
    {
        ambiguous = false;
        MethodInfo? bestMethod = null;
        var bestScore = -1;

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var argsWithCancellation = TryAppendCancellationToken(parameters, args, ct);
            var score = ScoreMethodMatch(parameters, argsWithCancellation);

            if (score > bestScore)
            {
                bestScore = score;
                bestMethod = method;
                ambiguous = false;
            }
            else if (score >= 0 && score == bestScore && bestMethod != null)
            {
                var tieBreak = CompareMethodSpecificity(bestMethod, method, args, ct);
                if (tieBreak < 0)
                {
                    bestMethod = method;
                    ambiguous = false;
                }
                else if (tieBreak == 0)
                {
                    ambiguous = true;
                }
            }
        }

        if (ambiguous)
            return null;

        return bestMethod;
    }

    private static int CompareMethodSpecificity(MethodInfo left, MethodInfo right, object?[] args, CancellationToken ct)
    {
        var leftParams = left.GetParameters();
        var rightParams = right.GetParameters();

        var leftIsParams = IsParamsMethod(leftParams);
        var rightIsParams = IsParamsMethod(rightParams);

        if (leftIsParams != rightIsParams)
            return leftIsParams ? -1 : 1;

        // Prefer non-generic methods over generic methods when other factors tie.
        if (left.IsGenericMethod != right.IsGenericMethod)
            return left.IsGenericMethod ? -1 : 1;

        var leftImplicitArgs = CountImplicitlySuppliedArgs(leftParams, args, ct);
        var rightImplicitArgs = CountImplicitlySuppliedArgs(rightParams, args, ct);
        if (leftImplicitArgs != rightImplicitArgs)
            return leftImplicitArgs < rightImplicitArgs ? 1 : -1;

        if (leftParams.Length != rightParams.Length)
            return leftParams.Length < rightParams.Length ? 1 : -1;

        var leftBetter = false;
        var rightBetter = false;
        var length = Math.Min(leftParams.Length, rightParams.Length);

        for (var i = 0; i < length; i++)
        {
            var leftType = GetSpecificityParameterType(leftParams, i);
            var rightType = GetSpecificityParameterType(rightParams, i);

            if (leftType == rightType)
                continue;

            if (leftType.IsAssignableFrom(rightType) && !rightType.IsAssignableFrom(leftType))
                rightBetter = true;
            else if (rightType.IsAssignableFrom(leftType) && !leftType.IsAssignableFrom(rightType))
                leftBetter = true;
        }

        return (leftBetter, rightBetter) switch
        {
            (true, false) => 1,
            (false, true) => -1,
            _ => 0
        };
    }

    private static Type GetSpecificityParameterType(ParameterInfo[] parameters, int index)
    {
        var parameterType = parameters[index].ParameterType;
        if (index == parameters.Length - 1 && parameters[index].IsDefined(typeof(ParamArrayAttribute), false))
            return parameterType.GetElementType() ?? parameterType;
        return parameterType;
    }

    private static bool IsParamsMethod(ParameterInfo[] parameters)
    {
        return parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute), false);
    }

    private static int CountImplicitlySuppliedArgs(ParameterInfo[] parameters, object?[] args, CancellationToken ct)
    {
        var argsWithCancellation = TryAppendCancellationToken(parameters, args, ct);
        var positionalArgs = new List<object?>();
        var namedArgs = new HashSet<string>(StringComparer.Ordinal);

        foreach (var arg in argsWithCancellation)
        {
            if (arg is NamedArg named)
                namedArgs.Add(named.Name);
            else
                positionalArgs.Add(arg);
        }

        var implicitCount = 0;
        var positionalIndex = 0;
        var lastParamIndex = parameters.Length - 1;
        var isParams = IsParamsMethod(parameters);

        for (var i = 0; i < parameters.Length; i++)
        {
            if (namedArgs.Contains(parameters[i].Name!))
                continue;

            if (positionalIndex < positionalArgs.Count)
            {
                positionalIndex++;
                continue;
            }

            if (parameters[i].HasDefaultValue)
            {
                implicitCount++;
                continue;
            }

            if (isParams && i == lastParamIndex)
            {
                implicitCount++;
                continue;
            }
        }

        return implicitCount;
    }

    private static object?[] PadWithDefaults(ParameterInfo[] parameters, object?[] args, string callableName)
    {
        if (parameters.Length == 0)
            return [];

        var lastParam = parameters[^1];
        var isParams = lastParam.IsDefined(typeof(ParamArrayAttribute), false);

        if (isParams)
            return PadWithParamsArray(parameters, args, lastParam, callableName);

        var result = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            if (i < args.Length)
                result[i] = TypeHelpers.CoerceNumeric(args[i], parameters[i].ParameterType);
            else if (parameters[i].HasDefaultValue)
                result[i] = parameters[i].DefaultValue;
            else
                throw new CsEvalException(
                    DiagnosticDescriptors.MissingRequiredArgument,
                    parameters[i].Name,
                    callableName);
        }

        return result;
    }

    private static object?[] PadWithParamsArray(ParameterInfo[] parameters, object?[] args, ParameterInfo paramsParam, string callableName)
    {
        var normalParamCount = parameters.Length - 1;
        var result = new object?[parameters.Length];

        for (var i = 0; i < normalParamCount; i++)
        {
            if (i < args.Length)
                result[i] = TypeHelpers.CoerceNumeric(args[i], parameters[i].ParameterType);
            else if (parameters[i].HasDefaultValue)
                result[i] = parameters[i].DefaultValue;
            else
                throw new CsEvalException(
                    DiagnosticDescriptors.MissingRequiredArgument,
                    parameters[i].Name,
                    callableName);
        }

        var paramsElementType = paramsParam.ParameterType.GetElementType()!;
        var paramsCount = Math.Max(0, args.Length - normalParamCount);
        var paramsArray = Array.CreateInstance(paramsElementType, paramsCount);

        for (var i = 0; i < paramsCount; i++)
        {
            var value = TypeHelpers.CoerceNumeric(args[normalParamCount + i], paramsElementType);
            paramsArray.SetValue(value, i);
        }

        result[normalParamCount] = paramsArray;
        return result;
    }

    /// <summary>
    /// After a method with ByRef parameters is invoked, copies the modified values from
    /// the converted args array back to the original args array for each OutArgMarker position.
    /// This allows the Evaluator to read the out parameter values from the original args array.
    /// </summary>
    private static void CopyBackOutArgs(object?[] originalArgs, object?[] convertedArgs, ParameterInfo[] parameters)
    {
        for (var i = 0; i < originalArgs.Length && i < parameters.Length; i++)
        {
            if (originalArgs[i] is OutArgMarker && parameters[i].ParameterType.IsByRef)
                originalArgs[i] = convertedArgs[i];
        }
    }

    internal static object? InvokeLambda(LambdaValue lambda, object?[] args, CsEvalContext context)
    {
        var childContext = lambda.Closure.CreateChild();
        for (var i = 0; i < lambda.Parameters.Count && i < args.Length; i++)
        {
            childContext.Define(lambda.Parameters[i], args[i]);
        }

        var evaluator = new Evaluator(childContext, lambda.Options);
        return evaluator.Evaluate(lambda.Body);
    }

    internal static object? InvokeCompiledLambda(CompiledLambdaValue lambda, object?[] args)
    {
        return lambda.CompiledBody(args, lambda.Closure);
    }
}
