using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Alder.Binding;
using Alder.Binding.Plans;
using Alder.Diagnostics;
using Alder.Interpretation;
using Alder.Parsing;

namespace Alder.Runtime;

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
        AlderContext context,
        AlderOptions options,
        CancellationToken ct,
        IReadOnlyList<string>? typeArgs = null)
    {
        if (nullSafe && target == null)
            return null;

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMethodCall, methodName);

        var result = TryInvokeInstanceMethod(target, methodName, args, context, options, ct, typeArgs);
        if (result.Success)
            return result.Value;

        var callee = MemberAccess.GetMember(target, methodName, options, nullSafe, context);
        return InvokeCall(callee, args, context, options, ct, typeArgs);
    }

    public static object? InvokeCall(
        object? callee,
        object?[] args,
        AlderContext context,
        AlderOptions options,
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
                !options.Sandbox.AllowMethodCalls
                    ? throw new AlderException(DiagnosticDescriptors.SandboxMethodCallBlocked, $"{staticRef.Type.Name}.{staticRef.MethodName}")
                : !options.Sandbox.IsTypeAllowed(staticRef.Type)
                    ? throw new AlderException(DiagnosticDescriptors.SandboxTypeBlocked, staticRef.Type.Name)
                : InvokeStaticMethod(staticRef.Type, staticRef.MethodName, args, context, options, typeArgs, ct),

            MethodRef methodRef =>
                InvokeMethodRef(methodRef, args, context, options, typeArgs, ct),

            // ── Unrecognized ────────────────────────────────────────────────
            null => throw new AlderException(DiagnosticDescriptors.NullInvocation),
            _ => throw new AlderException(DiagnosticDescriptors.NonCallableType, callee.GetType().Name)
        };
    }

    private static object? InvokeMethodRef(
        MethodRef methodRef,
        object?[] args,
        AlderContext context,
        AlderOptions options,
        IReadOnlyList<string>? typeArgs,
        CancellationToken ct)
    {
        var target = methodRef.Target;

        // System.Range / InclusiveRange → IEnumerable<int> for LINQ method calls
        if (target is Range or InclusiveRange)
            target = Extensions.RangeHelpers.EnsureEnumerable(target);

        var result = TryInvokeInstanceMethod(
            target, methodRef.MethodName, args,
            context, options, ct, typeArgs);
        if (result.Success)
            return result.Value;
        throw new AlderException(DiagnosticDescriptors.MethodInvocationFailed, methodRef.MethodName);
    }

    public static (bool Success, object? Value) TryInvokeInstanceMethod(
        object? target,
        string methodName,
        object?[] args,
        AlderContext context,
        AlderOptions options,
        CancellationToken ct,
        IReadOnlyList<string>? typeArgs = null)
    {
        if (target == null)
            return (false, null);

        if (target is ModuleInfo)
            return (false, null);

        // ECMA-334 §12.8.9.2: Instance methods take precedence over extension methods.
        if (options.Sandbox.AllowMethodCalls)
        {
            var type = target.GetType();

            if (!HasSpecialArgs(args) &&
                context.Config.AotMetadata is { } aotMeta && aotMeta.TryGetValue(type, out var metadata))
            {
                if (metadata.TryInvokeMethod(methodName, target, args, out var aotResult))
                    return (true, aotResult);
            }

            var flags = BindingFlags.Public | BindingFlags.Instance;
            if (!options.IsCaseSensitive)
                flags |= BindingFlags.IgnoreCase;
            var methods = context.TypeMetadata.GetMethods(type, methodName, flags);

            var resolved = ResolveMethod(methods, args, ct, typeArgs, context.TypeResolver, out var ambiguous);
            if (ambiguous)
                throw new AlderException(DiagnosticDescriptors.AmbiguousMethodInvocation, methodName);

            if (resolved != null)
            {
                var invokeResult = InvokeMethodWithArgs(resolved, target, args, ct);
                if (invokeResult.Success)
                    return invokeResult;
            }
        }

        // No applicable instance method found (or instance methods blocked).
        // Try extension methods per ECMA-334 §12.8.9.2.
        var extensionResult = ExtensionMethodResolver.TryInvokeExtensionMethod(
            target, methodName, args, context.ExtensionTypes, ct, options.IsCaseSensitive, typeArgs, context);
        if (extensionResult.Success)
            return extensionResult;

        if (!options.Sandbox.AllowMethodCalls)
            throw new AlderException(DiagnosticDescriptors.SandboxMethodCallBlocked, methodName);

        return (false, null);
    }

    /// <summary>
    /// Makes a generic method concrete using explicit type arguments.
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

        var resolved = ResolveMethod(methods, args, ct, typeArgs: null, typeResolver: null, out var ambiguous);
        if (ambiguous)
            throw new AlderException(DiagnosticDescriptors.AmbiguousMethodInvocation, methodName);

        resolved ??= methodRef.Method;

        var invokeResult = InvokeMethodWithArgs(resolved, target, args, ct);
        if (invokeResult.Success)
            return invokeResult.Value;

        throw new AlderException(DiagnosticDescriptors.MethodInvocationFailed, methodName);
    }

    private static object? InvokeStaticMethod(
        Type type,
        string methodName,
        object?[] args,
        AlderContext context,
        AlderOptions options,
        IReadOnlyList<string>? typeArgs,
        CancellationToken ct)
    {
        if (!HasSpecialArgs(args) &&
            context.Config.AotMetadata is { } aotMeta && aotMeta.TryGetValue(type, out var metadata))
        {
            if (metadata.TryInvokeStaticMethod(methodName, args, out var aotResult))
                return aotResult;
        }

        var bindingFlags = BindingFlags.Public | BindingFlags.Static;
        if (!options.IsCaseSensitive)
            bindingFlags |= BindingFlags.IgnoreCase;

        var methods = context.TypeMetadata.GetMethods(type, methodName, bindingFlags);

        var resolved = ResolveMethod(methods, args, ct, typeArgs, context.TypeResolver, out var ambiguous);
        if (ambiguous)
            throw new AlderException(DiagnosticDescriptors.AmbiguousMethodInvocation, $"{type.Name}.{methodName}");

        if (resolved != null)
        {
            var invokeResult = InvokeMethodWithArgs(resolved, null, args, ct);
            if (invokeResult.Success)
                return invokeResult.Value;
        }

        throw new AlderException(DiagnosticDescriptors.MemberNotFound, type.Name, methodName);
    }

    internal static (bool Success, object? Value) InvokeMethodWithArgs(
        MethodInfo method,
        object? target,
        object?[] args,
        CancellationToken ct)
    {
        var parameters = MethodDispatchCache.GetParameters(method);
        var argsWithCancellation = TryAppendCancellationToken(parameters, args, ct);

        // Fast path: exact simple invocation (no named args, no out/ref, no params, no conversions).
        if (CanInvokeDirect(parameters, argsWithCancellation))
            return (true, InvokeMethodCore(method, target, argsWithCancellation));

        if (CanInvokeMethod(parameters, argsWithCancellation, out var convertedArgs))
        {
            var result = InvokeMethodCore(method, target, convertedArgs);
            CopyBackOutArgs(args, convertedArgs, parameters);
            return (true, result);
        }

        return (false, null);
    }

    internal static (bool Success, object? Value) InvokePlannedMethod(
        BoundCallPlan plan,
        object? target,
        object?[] sourceArgs,
        CancellationToken ct)
    {
        var method = plan.SelectedMethod;
        var parameters = MethodDispatchCache.GetParameters(method);

        if (plan.ParameterBindings.Length != parameters.Length)
            return (false, null);

        if (plan.IsDirectArgumentMapping && sourceArgs.Length == parameters.Length)
            return (true, InvokeMethodCore(method, target, sourceArgs));

        if (!TryPreparePlannedArgumentsInPlace(plan, parameters, sourceArgs, out var preparedArgs) &&
            !TryBuildPlannedArguments(plan, parameters, sourceArgs, out preparedArgs))
        {
            return (false, null);
        }

        return (true, InvokeMethodCore(method, target, preparedArgs));
    }

    private static object? InvokeMethodCore(
        MethodInfo method,
        object? target,
        object?[] args)
    {
        try
        {
            object? result;
            if (!MethodDispatchCache.TryInvokeFast(method, target, args, out result))
                result = method.Invoke(target, args);

            return TypeHelpers.GuardReflectionLeak(result, $"method {method.Name}");
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    private static bool TryPreparePlannedArgumentsInPlace(
        BoundCallPlan plan,
        ParameterInfo[] parameters,
        object?[] sourceArgs,
        out object?[] preparedArgs)
    {
        preparedArgs = [];

        if (parameters.Length != sourceArgs.Length ||
            plan.ParameterBindings.Length != sourceArgs.Length ||
            plan.ArgumentConversions.Length != sourceArgs.Length)
        {
            return false;
        }

        for (var i = 0; i < plan.ParameterBindings.Length; i++)
        {
            var binding = plan.ParameterBindings[i];
            if (binding.Kind != BoundParameterBindingKind.Argument ||
                binding.ParameterIndex != i ||
                binding.SourceArgumentIndex != i ||
                binding.SourceArgumentCount != 1)
            {
                return false;
            }

            if (!TryConvertPlannedArgument(sourceArgs[i], plan.ArgumentConversions[i], out var converted))
                return false;

            sourceArgs[i] = converted;
        }

        preparedArgs = sourceArgs;
        return true;
    }

    private static bool TryBuildPlannedArguments(
        BoundCallPlan plan,
        ParameterInfo[] parameters,
        object?[] sourceArgs,
        out object?[] preparedArgs)
    {
        preparedArgs = new object?[parameters.Length];
        var conversions = plan.ArgumentConversions;

        for (var i = 0; i < plan.ParameterBindings.Length; i++)
        {
            var binding = plan.ParameterBindings[i];
            if ((uint)binding.ParameterIndex >= (uint)parameters.Length)
                return false;

            switch (binding.Kind)
            {
                case BoundParameterBindingKind.Argument:
                    if (!TryGetConvertedSourceArgument(
                            sourceArgs,
                            conversions,
                            binding.SourceArgumentIndex,
                            out var convertedArgument))
                    {
                        return false;
                    }

                    preparedArgs[binding.ParameterIndex] = convertedArgument;
                    break;

                case BoundParameterBindingKind.DefaultValue:
                    if (!TryGetDefaultParameterValue(parameters[binding.ParameterIndex], out var defaultValue))
                        return false;
                    preparedArgs[binding.ParameterIndex] = defaultValue;
                    break;

                case BoundParameterBindingKind.ParamsArray:
                    if (!TryCreatePlannedParamsArray(
                            parameters[binding.ParameterIndex],
                            sourceArgs,
                            conversions,
                            binding.SourceArgumentIndex,
                            binding.SourceArgumentCount,
                            out var paramsArray))
                    {
                        return false;
                    }

                    preparedArgs[binding.ParameterIndex] = paramsArray;
                    break;

                default:
                    return false;
            }
        }

        return true;
    }

    private static bool TryGetConvertedSourceArgument(
        object?[] sourceArgs,
        ImmutableArray<BoundConversionPlan> conversions,
        int sourceIndex,
        out object? converted)
    {
        converted = null;

        if ((uint)sourceIndex >= (uint)sourceArgs.Length)
            return false;

        if ((uint)sourceIndex >= (uint)conversions.Length)
            return false;

        var conversion = conversions[sourceIndex];
        var value = sourceArgs[sourceIndex];
        return TryConvertPlannedArgument(value, conversion, out converted);
    }

    private static bool TryConvertPlannedArgument(
        object? value,
        BoundConversionPlan conversion,
        out object? converted)
    {
        if (conversion.IsIdentity)
        {
            if (value == null)
            {
                if (conversion.TargetType.IsValueType && Nullable.GetUnderlyingType(conversion.TargetType) == null)
                {
                    converted = null;
                    return false;
                }

                converted = null;
                return true;
            }

            if (conversion.TargetType.IsInstanceOfType(value))
            {
                converted = value;
                return true;
            }
        }

        return TryConvertArg(value, conversion.TargetType, out converted);
    }

    private static bool TryGetDefaultParameterValue(ParameterInfo parameter, out object? value)
    {
        value = null;
        if (!parameter.HasDefaultValue)
            return false;

        var defaultValue = parameter.DefaultValue;
        if (defaultValue == Type.Missing || defaultValue == DBNull.Value)
        {
            value = TypeHelpers.GetDefaultValue(parameter.ParameterType);
            return true;
        }

        if (defaultValue == null &&
            parameter.ParameterType.IsValueType &&
            Nullable.GetUnderlyingType(parameter.ParameterType) == null)
        {
            value = TypeHelpers.GetDefaultValue(parameter.ParameterType);
            return true;
        }

        value = defaultValue;
        return true;
    }

    private static bool TryCreatePlannedParamsArray(
        ParameterInfo paramsParameter,
        object?[] sourceArgs,
        ImmutableArray<BoundConversionPlan> conversions,
        int startIndex,
        int count,
        out Array paramsArray)
    {
        paramsArray = Array.Empty<object>();

        if (count < 0 || startIndex < 0)
            return false;

        var elementType = paramsParameter.ParameterType.GetElementType();
        if (elementType == null)
            return false;

        if (startIndex + count > sourceArgs.Length || startIndex + count > conversions.Length)
            return false;

        paramsArray = RuntimeArrayFactory.Create(elementType, count);
        for (var i = 0; i < count; i++)
        {
            var sourceIndex = startIndex + i;
            if (!TryConvertPlannedArgument(sourceArgs[sourceIndex], conversions[sourceIndex], out var converted))
                return false;
            paramsArray.SetValue(converted, i);
        }

        return true;
    }

    private static bool CanInvokeDirect(
        ParameterInfo[] parameters,
        object?[] args)
    {
        if (parameters.Length != args.Length)
            return false;

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (parameter.ParameterType.IsByRef || parameter.IsDefined(typeof(ParamArrayAttribute), false))
                return false;

            var arg = args[i];
            if (arg is NamedArg or OutArgMarker)
                return false;

            if (arg == null)
            {
                if (parameter.ParameterType.IsValueType &&
                    Nullable.GetUnderlyingType(parameter.ParameterType) == null)
                {
                    return false;
                }
                continue;
            }

            var argType = arg.GetType();
            if (argType == parameter.ParameterType || parameter.ParameterType.IsAssignableFrom(argType))
                continue;

            return false;
        }

        return true;
    }

    private static MethodInfo? TryMakeConcreteMethod(MethodInfo genericMethod, object?[] args)
    {
        var genericArgs = genericMethod.GetGenericArguments();

        if (args.Length == 0)
            return null;

        var parameters = MethodDispatchCache.GetParameters(genericMethod);
        if (parameters.Length == 0)
            return null;

        try
        {
            var typeArgs = new Type[genericArgs.Length];
            var resolved = 0;

            for (var i = 0; i < parameters.Length && resolved < genericArgs.Length; i++)
            {
                var argType = i < args.Length ? args[i]?.GetType() : null;
                if (argType == null)
                    continue;

                resolved += TryInferTypeArgs(parameters[i].ParameterType, argType, genericArgs, typeArgs);
            }

            // All type args must be resolved — don't guess with typeof(object)
            for (var i = 0; i < typeArgs.Length; i++)
            {
                if (typeArgs[i] == null)
                    return null;
            }

            return RuntimeGenericFactory.TryCloseGenericMethod(genericMethod, typeArgs, out var closedMethod)
                ? closedMethod
                : null;
        }
        catch (Exception ex) when (ex is ArgumentException or TypeLoadException or InvalidOperationException)
        {
            return null;
        }
    }

    private static int TryInferTypeArgs(Type paramType, Type argType, Type[] genericParams, Type[] typeArgs)
    {
        var resolved = 0;

        if (paramType.IsGenericParameter)
        {
            var index = Array.IndexOf(genericParams, paramType);
            if (index >= 0 && typeArgs[index] == null)
            {
                typeArgs[index] = argType;
                resolved++;
            }
            return resolved;
        }

        if (paramType.IsGenericType)
        {
            var paramGenericDef = paramType.GetGenericTypeDefinition();
            var paramGenericArgs = paramType.GetGenericArguments();

            Type? matchingType = null;

            if (argType.IsGenericType && argType.GetGenericTypeDefinition() == paramGenericDef)
            {
                matchingType = argType;
            }
            else
            {
                foreach (var iface in ReflectionRuntime.GetInterfaces(argType))
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == paramGenericDef)
                    {
                        matchingType = iface;
                        break;
                    }
                }
            }

            if (matchingType != null)
            {
                var argGenericArgs = matchingType.GetGenericArguments();
                for (var i = 0; i < paramGenericArgs.Length && i < argGenericArgs.Length; i++)
                {
                    resolved += TryInferTypeArgs(paramGenericArgs[i], argGenericArgs[i], genericParams, typeArgs);
                }
            }
        }

        return resolved;
    }


    private static bool HasSpecialArgs(object?[] args)
    {
        foreach (var arg in args)
        {
            if (arg is NamedArg or OutArgMarker || arg == null)
                return true;
        }
        return false;
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
        SplitNamedAndPositionalArguments(args, out var positionalArgs, out var namedArgs);

        if (TryBindNormalForm(parameters, positionalArgs, namedArgs, out convertedArgs))
            return true;

        if (TryBindExpandedParamsForm(parameters, positionalArgs, namedArgs, out convertedArgs))
            return true;

        convertedArgs = [];
        return false;
    }

    private static void SplitNamedAndPositionalArguments(
        object?[] args,
        out List<object?> positionalArgs,
        out Dictionary<string, object?> namedArgs)
    {
        positionalArgs = new List<object?>(args.Length);
        namedArgs = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var arg in args)
        {
            if (arg is NamedArg named)
                namedArgs[named.Name] = named.Value;
            else
                positionalArgs.Add(arg);
        }
    }

    private static bool TryBindNormalForm(
        ParameterInfo[] parameters,
        List<object?> positionalArgs,
        Dictionary<string, object?> namedArgs,
        out object?[] convertedArgs)
    {
        convertedArgs = new object?[parameters.Length];
        var filledParams = new bool[parameters.Length];
        var positionalIndex = 0;

        for (var i = 0; i < parameters.Length && positionalIndex < positionalArgs.Count; i++)
        {
            if (namedArgs.ContainsKey(parameters[i].Name!))
                continue;

            var arg = positionalArgs[positionalIndex++];
            if (!TryConvertArgumentForParameter(arg, parameters[i], out var converted))
                return false;

            convertedArgs[i] = converted;
            filledParams[i] = true;
        }

        if (positionalIndex < positionalArgs.Count)
            return false;

        foreach (var (name, value) in namedArgs)
        {
            var paramIndex = FindParameterIndex(parameters, name);
            if (paramIndex < 0 || filledParams[paramIndex])
                return false;

            if (!TryConvertArgumentForParameter(value, parameters[paramIndex], out var converted))
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
                continue;
            }

            if (parameters[i].IsDefined(typeof(ParamArrayAttribute), false))
            {
                var elementType = parameters[i].ParameterType.GetElementType()!;
                convertedArgs[i] = RuntimeArrayFactory.Create(elementType, 0);
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool TryBindExpandedParamsForm(
        ParameterInfo[] parameters,
        List<object?> positionalArgs,
        Dictionary<string, object?> namedArgs,
        out object?[] convertedArgs)
    {
        convertedArgs = [];

        if (parameters.Length == 0)
            return false;

        var paramsIndex = parameters.Length - 1;
        var paramsParameter = parameters[paramsIndex];
        if (!paramsParameter.IsDefined(typeof(ParamArrayAttribute), false))
            return false;

        var paramsElementType = paramsParameter.ParameterType.GetElementType();
        if (paramsElementType == null)
            return false;

        convertedArgs = new object?[parameters.Length];
        var filledParams = new bool[parameters.Length];
        var positionalIndex = 0;

        for (var i = 0; i < paramsIndex; i++)
        {
            if (namedArgs.ContainsKey(parameters[i].Name!))
                continue;

            if (positionalIndex >= positionalArgs.Count)
                break;

            var arg = positionalArgs[positionalIndex++];
            if (!TryConvertArgumentForParameter(arg, parameters[i], out var converted))
                return false;

            convertedArgs[i] = converted;
            filledParams[i] = true;
        }

        var paramsProvidedByName = false;
        foreach (var (name, value) in namedArgs)
        {
            var paramIndex = FindParameterIndex(parameters, name);
            if (paramIndex < 0)
                return false;

            if (paramIndex == paramsIndex)
            {
                if (!TryConvertArgumentForParameter(value, parameters[paramIndex], out var namedParamsValue))
                    return false;

                convertedArgs[paramIndex] = namedParamsValue;
                paramsProvidedByName = true;
                continue;
            }

            if (filledParams[paramIndex])
                return false;

            if (!TryConvertArgumentForParameter(value, parameters[paramIndex], out var converted))
                return false;

            convertedArgs[paramIndex] = converted;
            filledParams[paramIndex] = true;
        }

        for (var i = 0; i < paramsIndex; i++)
        {
            if (filledParams[i])
                continue;

            if (!parameters[i].HasDefaultValue)
                return false;

            convertedArgs[i] = parameters[i].DefaultValue;
        }

        var remainingCount = positionalArgs.Count - positionalIndex;
        if (remainingCount < 0)
            return false;

        if (paramsProvidedByName)
            return remainingCount == 0;

        var paramsArray = RuntimeArrayFactory.Create(paramsElementType, remainingCount);
        for (var i = 0; i < remainingCount; i++)
        {
            var arg = positionalArgs[positionalIndex + i];
            if (!TryConvertArg(arg, paramsElementType, out var converted))
                return false;
            paramsArray.SetValue(converted, i);
        }

        convertedArgs[paramsIndex] = paramsArray;
        return true;
    }

    private static int FindParameterIndex(
        ParameterInfo[] parameters,
        string name)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            if (string.Equals(parameters[i].Name, name, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private static bool TryConvertArgumentForParameter(object? arg, ParameterInfo parameter, out object? converted)
    {
        if (arg is OutArgMarker && parameter.ParameterType.IsByRef)
        {
            var elementType = parameter.ParameterType.GetElementType()!;
            converted = TypeHelpers.GetDefaultValue(elementType);
            return true;
        }

        return TryConvertArg(arg, parameter.ParameterType, out converted);
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

        // Implicit numeric conversions (§10.2.3)
        if (TypeHelpers.IsStandardImplicitConversion(argType, targetType))
        {
            converted = TypeHelpers.CoerceNumeric(arg, targetType);
            return true;
        }

        // User-defined implicit conversion (§10.5.4)
        if (TypeHelpers.TryApplyUserDefinedImplicitConversion(arg, targetType, out var userConverted))
        {
            converted = userConverted;
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
    /// Finds the best matching method from candidates using ECMA-334 §12.6.4 pairwise elimination.
    /// Phase 1: filter to applicable candidates. Phase 2: pairwise comparison to find unique best.
    /// </summary>
    internal static MethodInfo? FindBestMethod(IEnumerable<MethodInfo> methods, object?[] args, CancellationToken ct, out bool ambiguous)
    {
        ambiguous = false;

        // Phase 1: collect applicable candidates
        var applicable = new List<(MethodInfo Method, ParameterInfo[] Params, ApplicableForm Form, object?[] ArgsWithCt)>();
        foreach (var method in methods)
        {
            var parameters = MethodDispatchCache.GetParameters(method);
            var argsWithCt = TryAppendCancellationToken(parameters, args, ct);

            if (OverloadResolution.IsApplicableForArgs(parameters, argsWithCt, out var form))
                applicable.Add((method, parameters, form, argsWithCt));
        }

        if (applicable.Count == 0)
            return null;
        if (applicable.Count == 1)
            return applicable[0].Method;

        // Phase 2: pairwise elimination
        var best = applicable[0];
        var bestIsUnique = true;

        for (var i = 1; i < applicable.Count; i++)
        {
            var challenger = applicable[i];
            var cmp = OverloadResolution.BetterFunctionMemberForArgs(
                best.Method, best.Params, best.Form,
                challenger.Method, challenger.Params, challenger.Form,
                best.ArgsWithCt);

            switch (cmp)
            {
                case BetterResult.Right:
                    best = challenger;
                    bestIsUnique = true;
                    break;
                case BetterResult.Neither:
                    bestIsUnique = false;
                    break;
            }
        }

        // Phase 3: verify winner beats all other candidates
        if (!bestIsUnique)
        {
            foreach (var candidate in applicable)
            {
                if (candidate.Method == best.Method)
                    continue;
                var cmp = OverloadResolution.BetterFunctionMemberForArgs(
                    best.Method, best.Params, best.Form,
                    candidate.Method, candidate.Params, candidate.Form,
                    best.ArgsWithCt);
                if (cmp != BetterResult.Left)
                {
                    ambiguous = true;
                    return null;
                }
            }
        }

        return best.Method;
    }

    /// <summary>
    /// Shared resolution pipeline: fast path → build candidate list → pairwise elimination.
    /// Used by both instance and static method dispatch.
    /// </summary>
    private static MethodInfo? ResolveMethod(
        MethodInfo[] methods,
        object?[] args,
        CancellationToken ct,
        IReadOnlyList<string>? typeArgs,
        TypeResolver? typeResolver,
        out bool ambiguous)
    {
        ambiguous = false;

        // Fast path: exact types, no named args, no nulls, no generic type args
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
                var fastMethod = MethodResolver.TryResolveMethod(methods, argTypes);
                if (fastMethod != null)
                    return fastMethod;
            }
        }

        // Build candidate list, closing generic methods where possible
        var candidates = new List<MethodInfo>();
        foreach (var method in methods)
        {
            if (!method.ContainsGenericParameters)
            {
                candidates.Add(method);
                continue;
            }

            MethodInfo? concrete = null;
            if (typeArgs is { Count: > 0 })
                concrete = TryMakeConcreteMethodWithTypeArgs(method, typeArgs, typeResolver);
            concrete ??= TryMakeConcreteMethod(method, args);
            if (concrete != null)
                candidates.Add(concrete);
        }

        return FindBestMethod(candidates, args, ct, out ambiguous);
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
                throw new AlderException(
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
                throw new AlderException(
                    DiagnosticDescriptors.MissingRequiredArgument,
                    parameters[i].Name,
                    callableName);
        }

        var paramsElementType = paramsParam.ParameterType.GetElementType()!;
        var paramsCount = Math.Max(0, args.Length - normalParamCount);
        var paramsArray = RuntimeArrayFactory.Create(paramsElementType, paramsCount);

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
    /// This allows lambda invocation to read the out parameter values from the original args array.
    /// </summary>
    private static void CopyBackOutArgs(object?[] originalArgs, object?[] convertedArgs, ParameterInfo[] parameters)
    {
        for (var i = 0; i < originalArgs.Length && i < parameters.Length; i++)
        {
            if (originalArgs[i] is OutArgMarker && parameters[i].ParameterType.IsByRef)
                originalArgs[i] = convertedArgs[i];
        }
    }

    internal static object? InvokeLambda(LambdaValue lambda, object?[] args, AlderContext context)
    {
        var childContext = lambda.Closure.CreateChild();
        for (var i = 0; i < lambda.Parameters.Count && i < args.Length; i++)
        {
            childContext.Define(lambda.Parameters[i], args[i]);
        }

        AstDepthValidator.EnsureWithinLimit(lambda.Body, lambda.Options?.MaxExpressionDepth ?? ExecutionConstraints.DefaultMaxExpressionDepth);
        var bound = lambda.GetOrBindBody(childContext);
        var evaluator = new BoundEvaluator(childContext, lambda.Options!);
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
