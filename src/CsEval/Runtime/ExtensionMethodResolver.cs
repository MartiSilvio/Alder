using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using CsEval.Interpretation;

namespace CsEval.Runtime;

internal static class ExtensionMethodResolver
{
    internal static (bool Success, object? Value) TryInvokeExtensionMethod(
        object target,
        string methodName,
        object?[] args,
        ImmutableArray<Type> extensionTypes,
        CancellationToken ct,
        bool isCaseSensitive,
        IReadOnlyList<string>? typeArgs = null,
        TypeResolver? resolver = null)
    {
        var targetType = target.GetType();

        foreach (var extType in extensionTypes)
        {
            var result = TryInvokeFromType(target, targetType, methodName, args, extType, ct, isCaseSensitive, typeArgs, resolver);
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
        bool isCaseSensitive,
        IReadOnlyList<string>? typeArgs = null,
        TypeResolver? resolver = null)
    {
        var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var methods = extensionType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name.Equals(methodName, comparison) &&
                        m.IsDefined(typeof(ExtensionAttribute), false))
            .ToList();

        var candidates = new List<MethodInfo>();
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
                if (typeArgs is { Count: > 0 })
                    concreteMethod = MethodInvoker.TryMakeConcreteMethodWithTypeArgs(method, typeArgs, resolver);
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
            candidates.Add(concreteMethod);
        }

        if (candidates.Count == 0)
            return (false, null);

        var invocationArgs = new object?[args.Length + 1];
        invocationArgs[0] = target;
        Array.Copy(args, 0, invocationArgs, 1, args.Length);

        (bool Success, object? Value) lambdaFallbackResult = (false, null);
        var best = MethodInvoker.FindBestMethod(candidates, invocationArgs, ct, out var ambiguous);
        if (ambiguous && !TryResolveLambdaSelectorAmbiguity(candidates, invocationArgs, ct, out lambdaFallbackResult))
            throw new CsEvalException($"Ambiguous method invocation: '{methodName}'");

        if (lambdaFallbackResult.Success)
            return lambdaFallbackResult;

        if (best == null)
            return (false, null);

        var invokeResult = MethodInvoker.InvokeMethodWithArgs(best, null, invocationArgs, ct);
        if (invokeResult.Success)
            return invokeResult;

        return (false, null);
    }

    private static bool TryResolveLambdaSelectorAmbiguity(
        List<MethodInfo> candidates,
        object?[] invocationArgs,
        CancellationToken ct,
        out (bool Success, object? Value) result)
    {
        result = (false, null);

        if (!HasLambdaArgument(invocationArgs))
            return false;

        // Deterministic fallback for selector-family overloads where reflection overload sets are broad.
        foreach (var candidate in candidates
                     .OrderBy(m => m.MetadataToken)
                     .ThenBy(m => m.GetParameters().Length))
        {
            var candidateResult = MethodInvoker.InvokeMethodWithArgs(candidate, null, invocationArgs, ct);
            if (candidateResult.Success)
            {
                result = candidateResult;
                return true;
            }
        }

        return true;
    }

    private static bool HasLambdaArgument(object?[] invocationArgs)
    {
        return invocationArgs.Skip(1).Any(a => a is LambdaValue or CompiledLambdaValue);
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
            if (testResult is null or MethodRef)
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

    private static bool IsFuncType(Type genericDef) =>
        genericDef == typeof(Func<>) ||
        genericDef == typeof(Func<,>) ||
        genericDef == typeof(Func<,,>) ||
        genericDef == typeof(Func<,,,>) ||
        genericDef == typeof(Func<,,,,>);
}
