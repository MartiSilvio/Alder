using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace CsEval.Runtime;

internal static class ExtensionMethodResolver
{
    private enum InvocationArgumentKind : byte
    {
        Null,
        RuntimeType,
        InterpretedLambda,
        CompiledLambda
    }

    private readonly record struct InvocationArgumentShape(
        InvocationArgumentKind Kind,
        Type? RuntimeType,
        int LambdaArity);

    private readonly record struct InvocationCacheKey(
        Type ExtensionType,
        Type TargetType,
        string MethodNameKey,
        bool IsCaseSensitive,
        string TypeArgSignature,
        ImmutableArray<InvocationArgumentShape> ArgumentShapes);

    private static readonly ConcurrentDictionary<(Type ExtensionType, string MethodNameKey, bool IsCaseSensitive), MethodInfo[]> ExtensionMethodsByNameCache = new();
    private static readonly ConcurrentDictionary<(Type ExtensionType, string MethodNameKey, bool IsCaseSensitive, int InvocationArgCount), MethodInfo[]> ExtensionMethodsByArityCache = new();
    private static readonly ConcurrentDictionary<MethodInfo, ParameterInfo[]> MethodParametersCache = new();
    private static readonly ConcurrentDictionary<InvocationCacheKey, ExtensionCallSitePlan?> ResolvedPlanByInvocationCache = new();

    private sealed record ExtensionCallSitePlan(MethodInfo Method);

    private static string NormalizeMethodName(string methodName, bool isCaseSensitive) =>
        isCaseSensitive ? methodName : methodName.ToUpperInvariant();

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
        var invocationArgs = new object?[args.Length + 1];
        invocationArgs[0] = target;
        Array.Copy(args, 0, invocationArgs, 1, args.Length);

        InvocationCacheKey? invocationCacheKey = null;
        if (TryCreateInvocationCacheKey(
                extensionType,
                targetType,
                methodName,
                args,
                isCaseSensitive,
                typeArgs,
                out var cacheKey))
        {
            invocationCacheKey = cacheKey;
            if (ResolvedPlanByInvocationCache.TryGetValue(cacheKey, out var cachedMethod))
            {
                if (cachedMethod == null)
                    return (false, null);

                var cachedInvokeResult = MethodInvoker.InvokeMethodWithArgs(cachedMethod.Method, null, invocationArgs, ct);
                return cachedInvokeResult.Success ? cachedInvokeResult : (false, null);
            }
        }

        var methods = GetExtensionMethodsForArity(extensionType, methodName, isCaseSensitive, invocationArgs.Length);
        var candidates = BuildCandidates(methods, targetType, args, typeArgs, resolver);

        if (candidates.Count == 0)
            return (false, null);

        (bool Success, object? Value) lambdaFallbackResult = (false, null);
        var best = MethodInvoker.FindBestMethod(candidates, invocationArgs, ct, out var ambiguous);
        if (ambiguous && !TryResolveLambdaSelectorAmbiguity(candidates, invocationArgs, ct, out lambdaFallbackResult))
            throw new CsEvalException($"Ambiguous method invocation: '{methodName}'");

        if (lambdaFallbackResult.Success)
            return lambdaFallbackResult;

        if (best == null)
        {
            if (invocationCacheKey is { } missingCacheKey)
                ResolvedPlanByInvocationCache.TryAdd(missingCacheKey, null);
            return (false, null);
        }

        if (invocationCacheKey is { } resolvedCacheKey)
            ResolvedPlanByInvocationCache.TryAdd(resolvedCacheKey, new ExtensionCallSitePlan(best));

        var invokeResult = MethodInvoker.InvokeMethodWithArgs(best, null, invocationArgs, ct);
        if (invokeResult.Success)
            return invokeResult;

        return (false, null);
    }

    private static MethodInfo[] GetExtensionMethods(Type extensionType, string methodName, bool isCaseSensitive)
    {
        var methodNameKey = NormalizeMethodName(methodName, isCaseSensitive);
        return ExtensionMethodsByNameCache.GetOrAdd(
            (extensionType, methodNameKey, isCaseSensitive),
            static key =>
            {
                var comparison = key.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                return ReflectionRuntime
                    .GetMethods(key.ExtensionType, BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name.Equals(key.MethodNameKey, comparison) &&
                                m.IsDefined(typeof(ExtensionAttribute), false))
                    .ToArray();
            });
    }

    private static MethodInfo[] GetExtensionMethodsForArity(
        Type extensionType,
        string methodName,
        bool isCaseSensitive,
        int invocationArgCount)
    {
        var methodNameKey = NormalizeMethodName(methodName, isCaseSensitive);
        return ExtensionMethodsByArityCache.GetOrAdd(
            (extensionType, methodNameKey, isCaseSensitive, invocationArgCount),
            static key =>
            {
                var methods = GetExtensionMethods(key.ExtensionType, key.MethodNameKey, key.IsCaseSensitive);
                var filtered = new List<MethodInfo>(methods.Length);
                foreach (var method in methods)
                {
                    var parameters = GetParameters(method);
                    if (CanParameterCountMatch(parameters, key.InvocationArgCount))
                        filtered.Add(method);
                }
                return filtered.ToArray();
            });
    }

    private static ParameterInfo[] GetParameters(MethodInfo method) =>
        MethodParametersCache.GetOrAdd(method, static m => m.GetParameters());

    private static bool CanParameterCountMatch(ParameterInfo[] parameters, int invocationArgCount)
    {
        if (parameters.Length == invocationArgCount)
            return true;

        var requiredCount = 0;
        foreach (var parameter in parameters)
        {
            if (parameter.IsDefined(typeof(ParamArrayAttribute), false) || parameter.HasDefaultValue)
                continue;
            requiredCount++;
        }

        if (invocationArgCount < requiredCount)
            return false;

        var hasParams = parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute), false);
        if (hasParams)
            return invocationArgCount >= parameters.Length - 1;

        return invocationArgCount <= parameters.Length;
    }

    private static bool TryCreateInvocationCacheKey(
        Type extensionType,
        Type targetType,
        string methodName,
        object?[] args,
        bool isCaseSensitive,
        IReadOnlyList<string>? typeArgs,
        out InvocationCacheKey key)
    {
        var argumentShapes = ImmutableArray.CreateBuilder<InvocationArgumentShape>(args.Length);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case NamedArg:
                case OutArgMarker:
                    key = default;
                    return false;
                case LambdaValue interpreted:
                    argumentShapes.Add(
                        new InvocationArgumentShape(
                            InvocationArgumentKind.InterpretedLambda,
                            RuntimeType: null,
                            LambdaArity: interpreted.Parameters.Count));
                    break;
                case CompiledLambdaValue compiled:
                    argumentShapes.Add(
                        new InvocationArgumentShape(
                            InvocationArgumentKind.CompiledLambda,
                            RuntimeType: null,
                            LambdaArity: compiled.Parameters.Count));
                    break;
                case null:
                    argumentShapes.Add(
                        new InvocationArgumentShape(
                            InvocationArgumentKind.Null,
                            RuntimeType: null,
                            LambdaArity: 0));
                    break;
                default:
                    argumentShapes.Add(
                        new InvocationArgumentShape(
                            InvocationArgumentKind.RuntimeType,
                            RuntimeType: arg.GetType(),
                            LambdaArity: 0));
                    break;
            }
        }

        var methodNameKey = NormalizeMethodName(methodName, isCaseSensitive);
        var typeArgSignature = typeArgs is { Count: > 0 }
            ? string.Join(",", typeArgs)
            : string.Empty;

        key = new InvocationCacheKey(
            extensionType,
            targetType,
            methodNameKey,
            isCaseSensitive,
            typeArgSignature,
            argumentShapes.ToImmutable());
        return true;
    }

    private static List<MethodInfo> BuildCandidates(
        IReadOnlyList<MethodInfo> methods,
        Type targetType,
        object?[] args,
        IReadOnlyList<string>? typeArgs,
        TypeResolver? resolver)
    {
        var candidates = new List<MethodInfo>(methods.Count);
        foreach (var method in methods)
        {
            var parameters = GetParameters(method);
            if (parameters.Length == 0)
                continue;

            var concreteMethod = TryBindConcreteMethod(method, targetType, args, typeArgs, resolver);
            if (concreteMethod == null)
                continue;

            var concreteParams = GetParameters(concreteMethod);
            if (!IsCompatible(targetType, concreteParams[0].ParameterType))
                continue;

            candidates.Add(concreteMethod);
        }

        return candidates;
    }

    private static MethodInfo? TryBindConcreteMethod(
        MethodInfo method,
        Type targetType,
        object?[] args,
        IReadOnlyList<string>? typeArgs,
        TypeResolver? resolver)
    {
        if (!method.ContainsGenericParameters)
            return method;

        if (typeArgs is { Count: > 0 })
            return MethodInvoker.TryMakeConcreteMethodWithTypeArgs(method, typeArgs, resolver);

        return TryMakeConcreteMethod(method, targetType, args);
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

            return RuntimeGenericFactory.CloseGenericMethod(genericMethod, typeArgs);
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
            {
                // Execution failed — infer return type statically from the lambda body AST
                var staticType = TryInferLambdaReturnTypeStatically(arg, substitutedInputTypes);
                if (staticType != null && staticType != typeof(object))
                {
                    inferred = staticType;
                    return true;
                }
                continue;
            }

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
            return RuntimeGenericFactory.CloseGenericType(type.GetGenericTypeDefinition(), args);
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
            return TypeHelpers.GetDefaultValue(type);
        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            var arr = RuntimeArrayFactory.Create(elementType, 1);
            arr.SetValue(CreateDefaultValue(elementType), 0);
            return arr;
        }
        if (type == typeof(string))
            return "";
        if (type is { IsAbstract: false, IsInterface: false })
        {
            try
            {
                return RuntimeHelpers.GetUninitializedObject(type);
            }
            catch
            {
            }
        }
        return null;
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
            foreach (var iface in ReflectionRuntime.GetInterfaces(actualType))
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

    private static Type? TryInferLambdaReturnTypeStatically(object? arg, Type[] inputTypes)
    {
        if (arg is not LambdaValue lambda)
            return null;

        var paramTypes = new Dictionary<string, Type>();
        for (var i = 0; i < lambda.Parameters.Count && i < inputTypes.Length; i++)
            paramTypes[lambda.Parameters[i]] = inputTypes[i];

        return InferExprType(lambda.Body, paramTypes);
    }

    private static Type? InferExprType(Parsing.Expr expr, Dictionary<string, Type> paramTypes)
    {
        switch (expr)
        {
            case Parsing.LiteralExpr literal:
                return literal.Value?.GetType() ?? typeof(object);

            case Parsing.IdentifierExpr id:
                return paramTypes.TryGetValue(id.Name.Lexeme, out var t) ? t : null;

            case Parsing.CallExpr { Callee: Parsing.MemberAccessExpr memberAccess } call:
            {
                var targetType = InferExprType(memberAccess.Object, paramTypes);
                if (targetType == null)
                    return null;

                var methodName = memberAccess.Name.Lexeme;
                var methods = ReflectionRuntime.GetMethods(targetType, BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.Name == methodName && m.GetParameters().Length == call.Arguments.Count)
                    .ToArray();

                return methods.Length > 0 ? methods[0].ReturnType : null;
            }

            case Parsing.MemberAccessExpr memberAccess:
            {
                var targetType = InferExprType(memberAccess.Object, paramTypes);
                if (targetType == null)
                    return null;

                var prop = targetType.GetProperty(memberAccess.Name.Lexeme, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null) return prop.PropertyType;

                var field = targetType.GetField(memberAccess.Name.Lexeme, BindingFlags.Public | BindingFlags.Instance);
                return field?.FieldType;
            }

            case Parsing.BinaryExpr binary:
            {
                var leftType = InferExprType(binary.Left, paramTypes);
                var rightType = InferExprType(binary.Right, paramTypes);
                if (leftType == typeof(string) || rightType == typeof(string))
                    return typeof(string);
                return leftType ?? rightType;
            }

            case Parsing.CastExpr:
                return null;

            case Parsing.ConditionalExpr cond:
                return InferExprType(cond.ThenBranch, paramTypes)
                    ?? InferExprType(cond.ElseBranch, paramTypes);

            default:
                return null;
        }
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
        foreach (var iface in ReflectionRuntime.GetInterfaces(sourceType))
        {
            if (targetType.IsAssignableFrom(iface))
                return true;
        }

        return false;
    }

    private static readonly HashSet<Type> FuncTypeDefinitions =
    [
        typeof(Func<>),
        typeof(Func<,>),
        typeof(Func<,,>),
        typeof(Func<,,,>),
        typeof(Func<,,,,>),
        typeof(Func<,,,,,>),
        typeof(Func<,,,,,,>),
        typeof(Func<,,,,,,,>),
        typeof(Func<,,,,,,,,>),
        typeof(Func<,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,,,,,,>),
    ];

    private static bool IsFuncType(Type genericDef) => FuncTypeDefinitions.Contains(genericDef);
}
