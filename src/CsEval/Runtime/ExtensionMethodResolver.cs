using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using CsEval.Interpretation;

namespace CsEval.Runtime;

public static class ExtensionMethodResolver
{
    public static (bool Success, object? Value) TryInvokeExtensionMethod(
        object target,
        string methodName,
        object?[] args,
        ImmutableArray<Type> extensionTypes,
        CancellationToken ct,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer,
        IReadOnlyList<string>? typeArgs = null)
    {
        var targetType = target.GetType();

        foreach (var extType in extensionTypes)
        {
            var result = TryInvokeFromType(target, targetType, methodName, args, extType, ct, argumentTransformer, typeArgs);
            if (result.Success)
                return result;
        }

        return (false, null);
    }

    private static (bool Success, object? Value) TryInvokeFromType(
        object target,
        Type targetType,
        string methodName,
        object?[] args,
        Type extensionType,
        CancellationToken ct,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer,
        IReadOnlyList<string>? typeArgs = null)
    {
        var methods = extensionType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase) &&
                        m.IsDefined(typeof(ExtensionAttribute), false))
            .ToList();

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
                continue;

            // Try to make the method concrete if it's generic
            MethodInfo? concreteMethod;
            if (method.ContainsGenericParameters)
            {
                // If explicit type arguments provided, use them
                if (typeArgs != null && typeArgs.Count > 0)
                    concreteMethod = TryMakeConcreteMethodWithExplicitArgs(method, typeArgs);
                else
                    concreteMethod = TryMakeConcreteMethod(method, targetType, args);
            }
            else
            {
                concreteMethod = method;
            }

            if (concreteMethod == null)
                continue;

            var concreteParams = concreteMethod.GetParameters();

            // Check if first parameter is compatible with target type
            if (!IsCompatible(targetType, concreteParams[0].ParameterType))
                continue;

            // Build full args array: [target, ...args]
            var fullArgs = BuildFullArgs(target, args, concreteParams, ct);
            if (fullArgs == null)
                continue;

            // Try to invoke
            if (TryInvoke(concreteMethod, fullArgs, argumentTransformer, out var result))
                return (true, result);
        }

        return (false, null);
    }

    private static MethodInfo? TryMakeConcreteMethodWithExplicitArgs(MethodInfo genericMethod, IReadOnlyList<string> typeArgs)
    {
        var genericParams = genericMethod.GetGenericArguments();
        if (genericParams.Length != typeArgs.Count)
            return null;

        try
        {
            var resolvedTypes = new Type[typeArgs.Count];
            for (var i = 0; i < typeArgs.Count; i++)
            {
                var type = MethodInvoker.ResolveTypeName(typeArgs[i]);
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

    private static MethodInfo? TryMakeConcreteMethod(MethodInfo genericMethod, Type targetType, object?[] args)
    {
        var genericParams = genericMethod.GetGenericArguments();
        var methodParams = genericMethod.GetParameters();

        if (methodParams.Length == 0)
            return null;

        try
        {
            var typeArgs = new Type[genericParams.Length];
            var resolved = 0;

            for (var i = 0; i < methodParams.Length && resolved < genericParams.Length; i++)
            {
                var paramType = methodParams[i].ParameterType;
                var argType = i == 0 ? targetType : (args.Length > i - 1 ? args[i - 1]?.GetType() : null);

                if (argType == null)
                    continue;

                resolved += TryInferTypeArg(paramType, argType, genericParams, typeArgs);
            }

            for (var i = 0; i < typeArgs.Length; i++)
            {
                if (typeArgs[i] != null)
                    continue;

                if (TryInferFromLambdaResult(methodParams, args, genericParams, typeArgs, i, out var inferred))
                {
                    typeArgs[i] = inferred;
                    resolved++;
                }
                else
                {
                    typeArgs[i] = typeof(object);
                }
            }

            var result = genericMethod.MakeGenericMethod(typeArgs);
            return result;
        }
        catch (Exception ex) when (ex is ArgumentException or TypeLoadException or InvalidOperationException)
        {
            return null;
        }
    }

    private static bool TryInferFromLambdaResult(
        ParameterInfo[] methodParams,
        object?[] args,
        Type[] genericParams,
        Type[] typeArgs,
        int targetIndex,
        out Type inferred)
    {
        inferred = typeof(object);
        var genericParam = genericParams[targetIndex];

        for (var i = 1; i < methodParams.Length && i - 1 < args.Length; i++)
        {
            var param = methodParams[i];
            var arg = args[i - 1];

            if (arg == null)
                continue;

            if (!param.ParameterType.IsGenericType)
                continue;

            var paramGenericDef = param.ParameterType.GetGenericTypeDefinition();
            if (!IsFuncType(paramGenericDef))
                continue;

            var paramGenericArgs = param.ParameterType.GetGenericArguments();
            var resultIndex = paramGenericArgs.Length - 1;
            var expectedResultType = paramGenericArgs[resultIndex];

            // Check if the result type involves the generic param
            Type? wrapperGenericDef = null;
            if (expectedResultType.Equals(genericParam))
            {
                // Direct: Func<T, TResult> where we want TResult
            }
            else if (expectedResultType.IsGenericType && ContainsGenericParam(expectedResultType, genericParam))
            {
                // Wrapped: Func<T, IEnumerable<TResult>> where we want TResult
                wrapperGenericDef = expectedResultType.GetGenericTypeDefinition();
            }
            else
            {
                continue;
            }

            // Create proper test args based on the Func's input types
            var inputTypes = paramGenericArgs.Take(paramGenericArgs.Length - 1).ToArray();
            var substitutedInputTypes = SubstituteTypeArgs(inputTypes, genericParams, typeArgs);
            var testArgs = CreateTypedDefaultArgs(substitutedInputTypes);

            var testResult = TryInvokeLambdaForTypeInference(arg, testArgs);
            if (testResult == null || testResult is MethodRef)
                continue;

            var resultType = testResult.GetType();

            if (wrapperGenericDef != null)
            {
                // Extract the inner type from the wrapper (e.g., IEnumerable<int> -> int)
                var extracted = ExtractTypeArgFromWrapper(resultType, wrapperGenericDef, expectedResultType, genericParam);
                if (extracted != null)
                {
                    inferred = extracted;
                    return true;
                }
            }
            else
            {
                inferred = resultType;
                return true;
            }
        }

        return false;
    }

    private static bool ContainsGenericParam(Type type, Type genericParam)
    {
        if (type.Equals(genericParam))
            return true;
        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                if (ContainsGenericParam(arg, genericParam))
                    return true;
            }
        }
        return false;
    }

    private static Type[] SubstituteTypeArgs(Type[] types, Type[] genericParams, Type[] typeArgs)
    {
        return types.Select(t => SubstituteTypeArg(t, genericParams, typeArgs)).ToArray();
    }

    private static Type SubstituteTypeArg(Type type, Type[] genericParams, Type[] typeArgs)
    {
        if (type.IsGenericParameter)
        {
            var index = Array.IndexOf(genericParams, type);
            if (index >= 0 && typeArgs[index] != null)
                return typeArgs[index];
            return typeof(object);
        }
        if (type.IsGenericType)
        {
            var args = type.GetGenericArguments()
                .Select(t => SubstituteTypeArg(t, genericParams, typeArgs))
                .ToArray();
            return type.GetGenericTypeDefinition().MakeGenericType(args);
        }
        return type;
    }

    private static object?[] CreateTypedDefaultArgs(Type[] types)
    {
        var args = new object?[types.Length];
        for (var i = 0; i < types.Length; i++)
            args[i] = CreateDefaultValue(types[i]);
        return args;
    }

    private static object? CreateDefaultValue(Type type)
    {
        if (type.IsValueType)
            return Activator.CreateInstance(type);
        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            var arr = Array.CreateInstance(elementType, 1);
            arr.SetValue(CreateDefaultValue(elementType), 0);
            return arr;
        }
        if (type == typeof(string))
            return "";
        try
        {
            return Activator.CreateInstance(type);
        }
        catch (Exception ex) when (ex is MissingMethodException or MethodAccessException or MemberAccessException)
        {
            return null;
        }
    }

    private static Type? ExtractTypeArgFromWrapper(Type actualType, Type wrapperGenericDef, Type expectedType, Type genericParam)
    {
        // actualType might be SelectArrayIterator<int, int> which implements IEnumerable<int>
        // wrapperGenericDef is IEnumerable<>
        // expectedType is IEnumerable<TResult>
        // genericParam is TResult
        // We want to return int

        Type? matchingType = null;
        if (actualType.IsGenericType && actualType.GetGenericTypeDefinition() == wrapperGenericDef)
        {
            matchingType = actualType;
        }
        else
        {
            foreach (var iface in actualType.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == wrapperGenericDef)
                {
                    matchingType = iface;
                    break;
                }
            }
        }

        if (matchingType == null)
            return null;

        // Find which position in expectedType has genericParam, and get that position from matchingType
        var expectedArgs = expectedType.GetGenericArguments();
        var actualArgs = matchingType.GetGenericArguments();

        for (var i = 0; i < expectedArgs.Length && i < actualArgs.Length; i++)
        {
            if (expectedArgs[i].Equals(genericParam))
                return actualArgs[i];
        }

        return null;
    }

    private static object? TryInvokeLambdaForTypeInference(object? arg, object?[] testArgs)
    {
        try
        {
            return arg switch
            {
                LambdaValue lambda => MethodInvoker.InvokeLambda(lambda, testArgs, lambda.Closure),
                CompiledLambdaValue compiled => MethodInvoker.InvokeCompiledLambda(compiled, testArgs),
                _ => null
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static int TryInferTypeArg(Type paramType, Type argType, Type[] genericParams, Type[] typeArgs)
    {
        var resolved = 0;

        // Direct generic parameter: T
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

        // Generic type like IEnumerable<T>, Func<T, TResult>, etc.
        if (paramType.IsGenericType)
        {
            var paramGenericDef = paramType.GetGenericTypeDefinition();
            var paramGenericArgs = paramType.GetGenericArguments();

            // Try to find matching interface on argType
            Type? matchingType = null;

            if (argType.IsGenericType && argType.GetGenericTypeDefinition() == paramGenericDef)
            {
                matchingType = argType;
            }
            else
            {
                foreach (var iface in argType.GetInterfaces())
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
                    resolved += TryInferTypeArg(paramGenericArgs[i], argGenericArgs[i], genericParams, typeArgs);
                }
            }
        }

        return resolved;
    }

    private static bool IsCompatible(Type sourceType, Type targetType)
    {
        if (targetType.IsAssignableFrom(sourceType))
            return true;

        // Check interfaces
        foreach (var iface in sourceType.GetInterfaces())
        {
            if (targetType.IsAssignableFrom(iface))
                return true;
        }

        return false;
    }

    private static object?[]? BuildFullArgs(
        object target,
        object?[] args,
        ParameterInfo[] parameters,
        CancellationToken ct)
    {
        // Build: [target, args..., optional defaults, optional cancellation token]
        var fullArgs = new object?[parameters.Length];
        fullArgs[0] = target;

        var argsIndex = 0;
        for (var i = 1; i < parameters.Length; i++)
        {
            var param = parameters[i];

            // Handle CancellationToken
            if (param.ParameterType == typeof(CancellationToken))
            {
                fullArgs[i] = ct;
                continue;
            }

            // Use provided arg if available
            if (argsIndex < args.Length)
            {
                var arg = args[argsIndex++];
                if (!TryConvertArg(arg, param.ParameterType, out var converted))
                    return null;
                fullArgs[i] = converted;
            }
            else if (param.HasDefaultValue)
            {
                fullArgs[i] = param.DefaultValue;
            }
            else
            {
                return null; // Missing required argument
            }
        }

        // Check we used all args
        if (argsIndex < args.Length)
            return null;

        return fullArgs;
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

        if (targetType.IsAssignableFrom(argType))
        {
            converted = arg;
            return true;
        }

        if (arg is Delegate del && targetType.IsAssignableFrom(del.GetType()))
        {
            converted = del;
            return true;
        }

        if (targetType.IsGenericType && IsFuncType(targetType.GetGenericTypeDefinition()))
        {
            if (TryConvertToFunc(arg, targetType, out converted))
                return true;
        }

        try
        {
            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (arg is IConvertible)
            {
                converted = Convert.ChangeType(arg, underlying);
                return true;
            }
        }
        catch (Exception ex) when (ex is InvalidCastException or OverflowException or FormatException)
        {
        }

        return false;
    }

    private static bool IsFuncType(Type genericDef) =>
        genericDef == typeof(Func<>) ||
        genericDef == typeof(Func<,>) ||
        genericDef == typeof(Func<,,>) ||
        genericDef == typeof(Func<,,,>) ||
        genericDef == typeof(Func<,,,,>);

    private static bool TryConvertToFunc(object arg, Type targetFuncType, out object? converted)
    {
        converted = null;

        switch (arg)
        {
            case LambdaValue lambda:
                converted = CreateDelegateFromLambda(lambda, targetFuncType);
                return converted != null;

            case CompiledLambdaValue compiled:
                converted = CreateDelegateFromCompiled(compiled, targetFuncType);
                return converted != null;

            default:
                return false;
        }
    }

    private static object? CreateDelegateFromLambda(LambdaValue lambda, Type targetFuncType)
    {
        var typeArgs = targetFuncType.GetGenericArguments();
        var paramCount = typeArgs.Length - 1;

        if (lambda.Parameters.Count != paramCount)
            return null;

        return paramCount switch
        {
            1 => CreateFunc1(lambda, typeArgs[0], typeArgs[1]),
            2 => CreateFunc2(lambda, typeArgs[0], typeArgs[1], typeArgs[2]),
            _ => null
        };
    }

    private static object CreateFunc1(LambdaValue lambda, Type t1, Type tResult)
    {
        Func<object?, object?> wrapper = arg =>
            MethodInvoker.InvokeLambda(lambda, [arg], lambda.Closure);

        var genericMethod = typeof(ExtensionMethodResolver)
            .GetMethod(nameof(WrapFunc1), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(t1, tResult);

        return genericMethod.Invoke(null, [wrapper])!;
    }

    private static object CreateFunc2(LambdaValue lambda, Type t1, Type t2, Type tResult)
    {
        Func<object?, object?, object?> wrapper = (arg1, arg2) =>
            MethodInvoker.InvokeLambda(lambda, [arg1, arg2], lambda.Closure);

        var genericMethod = typeof(ExtensionMethodResolver)
            .GetMethod(nameof(WrapFunc2), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(t1, t2, tResult);

        return genericMethod.Invoke(null, [wrapper])!;
    }

    private static Func<T1, TResult> WrapFunc1<T1, TResult>(Func<object?, object?> inner) =>
        arg => (TResult)inner(arg)!;

    private static Func<T1, T2, TResult> WrapFunc2<T1, T2, TResult>(Func<object?, object?, object?> inner) =>
        (arg1, arg2) => (TResult)inner(arg1, arg2)!;

    private static object? CreateDelegateFromCompiled(CompiledLambdaValue compiled, Type targetFuncType)
    {
        var typeArgs = targetFuncType.GetGenericArguments();
        var paramCount = typeArgs.Length - 1;

        if (compiled.Parameters.Count != paramCount)
            return null;

        return paramCount switch
        {
            1 => CreateCompiledFunc1(compiled, typeArgs[0], typeArgs[1]),
            2 => CreateCompiledFunc2(compiled, typeArgs[0], typeArgs[1], typeArgs[2]),
            _ => null
        };
    }

    private static object CreateCompiledFunc1(CompiledLambdaValue compiled, Type t1, Type tResult)
    {
        Func<object?, object?> wrapper = arg =>
            MethodInvoker.InvokeCompiledLambda(compiled, [arg]);

        var genericMethod = typeof(ExtensionMethodResolver)
            .GetMethod(nameof(WrapFunc1), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(t1, tResult);

        return genericMethod.Invoke(null, [wrapper])!;
    }

    private static object CreateCompiledFunc2(CompiledLambdaValue compiled, Type t1, Type t2, Type tResult)
    {
        Func<object?, object?, object?> wrapper = (arg1, arg2) =>
            MethodInvoker.InvokeCompiledLambda(compiled, [arg1, arg2]);

        var genericMethod = typeof(ExtensionMethodResolver)
            .GetMethod(nameof(WrapFunc2), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(t1, t2, tResult);

        return genericMethod.Invoke(null, [wrapper])!;
    }

    private static bool TryInvoke(
        MethodInfo method,
        object?[] args,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer,
        out object? result)
    {
        result = null;

        try
        {
            var finalArgs = argumentTransformer != null ? argumentTransformer(method, args) : args;
            result = method.Invoke(null, finalArgs);

            // Guard against reflection type leaks
            if (result != null && TypeHelpers.IsForbiddenReflectionType(result.GetType()))
                throw new CsEvalException($"Access to reflection types is not allowed: {result.GetType().Name}");

            return true;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
        catch (CsEvalException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CsEvalException($"Extension method invocation failed: {ex.Message} ({ex.GetType().Name})");
        }
    }
}
