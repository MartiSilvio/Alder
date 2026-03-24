using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Alder.Diagnostics;

namespace Alder.Runtime;

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
    private static readonly ConcurrentDictionary<InvocationCacheKey, ExtensionCallSitePlan?> ResolvedPlanByInvocationCache = new();
    private static readonly ConcurrentQueue<InvocationCacheKey> _resolvedPlanInsertionOrder = new();
    private const int MaxResolvedPlanCacheSize = 4096;

    private sealed record ExtensionCallSitePlan(MethodInfo Method);

    private static void CacheResolvedPlan(InvocationCacheKey key, ExtensionCallSitePlan? plan)
    {
        if (ResolvedPlanByInvocationCache.TryAdd(key, plan))
        {
            _resolvedPlanInsertionOrder.Enqueue(key);
            while (ResolvedPlanByInvocationCache.Count > MaxResolvedPlanCacheSize &&
                   _resolvedPlanInsertionOrder.TryDequeue(out var oldest))
            {
                ResolvedPlanByInvocationCache.TryRemove(oldest, out _);
            }
        }
    }

    private static string NormalizeMethodName(string methodName, bool isCaseSensitive) =>
        isCaseSensitive ? methodName : methodName.ToUpperInvariant();

    internal static (bool Success, object? Value) TryInvokeExtensionMethod(
        object target,
        string methodName,
        object?[] args,
        ImmutableArray<Type> extensionTypes,
        bool isCaseSensitive,
        IReadOnlyList<string>? typeArgs = null,
        AlderContext? runtimeContext = null,
        CancellationToken ct = default)
    {
        var targetType = target.GetType();

        foreach (var extType in extensionTypes)
        {
            var result = TryInvokeFromType(target, targetType, methodName, args, extType, isCaseSensitive, typeArgs, runtimeContext, ct);
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
        bool isCaseSensitive,
        IReadOnlyList<string>? typeArgs = null,
        AlderContext? runtimeContext = null,
        CancellationToken ct = default)
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
        var candidates = BuildCandidates(methods, targetType, args, typeArgs, runtimeContext);

        if (candidates.Count == 0)
            return (false, null);

        var best = MethodInvoker.FindBestMethod(candidates, invocationArgs, out var ambiguous, ct, runtimeContext);

        if (ambiguous)
            throw new AlderException(DiagnosticDescriptors.AmbiguousMethodInvocation, methodName);

        if (best == null)
        {
            if (invocationCacheKey is { } missingCacheKey)
                CacheResolvedPlan(missingCacheKey, null);
            return (false, null);
        }

        if (invocationCacheKey is { } resolvedCacheKey)
            CacheResolvedPlan(resolvedCacheKey, new ExtensionCallSitePlan(best));

        var invokeResult = MethodInvoker.InvokeMethodWithArgs(best, null, invocationArgs, ct);
        if (invokeResult.Success)
            return invokeResult;

        return (false, null);
    }

    private static MethodInfo[] GetExtensionMethods(Type extensionType, string methodName, bool isCaseSensitive)
    {
        var methodNameKey = NormalizeMethodName(methodName, isCaseSensitive);
        return GetExtensionMethodsByNormalizedName(extensionType, methodNameKey, isCaseSensitive);
    }

    private static MethodInfo[] GetExtensionMethodsByNormalizedName(Type extensionType, string methodNameKey, bool isCaseSensitive)
    {
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
                var methods = GetExtensionMethodsByNormalizedName(key.ExtensionType, key.MethodNameKey, key.IsCaseSensitive);
                var filtered = new List<MethodInfo>(methods.Length);
                foreach (var method in methods)
                {
                    var parameters = MethodDispatchCache.GetParameters(method);
                    if (CanParameterCountMatch(parameters, key.InvocationArgCount))
                        filtered.Add(method);
                }
                return filtered.ToArray();
            });
    }

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
        AlderContext? runtimeContext)
    {
        var candidates = new List<MethodInfo>(methods.Count);
        foreach (var method in methods)
        {
            var parameters = MethodDispatchCache.GetParameters(method);
            if (parameters.Length == 0)
                continue;

            var concreteMethod = TryBindConcreteMethod(method, targetType, args, typeArgs, runtimeContext);
            if (concreteMethod == null)
                continue;

            var concreteParams = MethodDispatchCache.GetParameters(concreteMethod);
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
        AlderContext? runtimeContext)
    {
        if (!method.ContainsGenericParameters)
            return method;

        if (typeArgs is { Count: > 0 })
            return MethodInvoker.TryMakeConcreteMethodWithTypeArgs(method, typeArgs, runtimeContext?.TypeResolver);

        return TryMakeConcreteMethod(method, targetType, args, runtimeContext);
    }

    private static MethodInfo? TryMakeConcreteMethod(MethodInfo genericMethod, Type targetType, object?[] args, AlderContext? runtimeContext = null)
    {
        var parameters = genericMethod.GetParameters();
        if (parameters.Length == 0)
            return null;

        var argTypes = new Type?[parameters.Length];
        argTypes[0] = targetType;
        for (var i = 1; i < parameters.Length && i - 1 < args.Length; i++)
            argTypes[i] = args[i - 1]?.GetType();

        object?[]? lambdaArgs = null;
        for (var i = 1; i < parameters.Length && i - 1 < args.Length; i++)
        {
            if (args[i - 1] is LambdaValue or CompiledLambdaValue)
            {
                lambdaArgs ??= new object?[parameters.Length];
                lambdaArgs[i] = args[i - 1];
            }
        }

        var typeArgs = TypeInference.Infer(genericMethod, argTypes, lambdaArgs, runtimeContext);
        if (typeArgs == null)
            return null;

        return RuntimeGenericFactory.TryCloseGenericMethod(genericMethod, typeArgs, out var closed)
            ? closed : null;
    }

    internal static Type? InferLambdaReturnType(object? arg, Type[] inputTypes, AlderContext? runtimeContext)
        => TryInferLambdaReturnTypeStatically(arg, inputTypes, runtimeContext);

    private static Type? TryInferLambdaReturnTypeStatically(object? arg, Type[] inputTypes, AlderContext? runtimeContext)
    {
        if (runtimeContext == null)
            return null;

        IReadOnlyList<string>? parameters;
        Parsing.Expr? body;

        switch (arg)
        {
            case LambdaValue lambda:
                parameters = lambda.Parameters;
                body = lambda.Body;
                break;
            case CompiledLambdaValue { Source: not null } compiled:
                parameters = compiled.Source.Parameters.Select(static p => p.Name.Lexeme).ToList();
                body = compiled.Source.Body;
                break;
            default:
                return null;
        }

        try
        {
            var bindingContext = new Binding.BindingContext(runtimeContext).CreateChildScope();
            for (var i = 0; i < parameters.Count && i < inputTypes.Length; i++)
                bindingContext.DeclareLocal(parameters[i], inputTypes[i]);

            var binder = new Binding.Binder();
            var bound = binder.Bind(body, bindingContext);

            if (bound.HasErrors)
                return null;

            // For block bodies, prefer a concrete return type over the block's overall type
            if (bound is Binding.BoundNodes.BoundBlockExpr block)
            {
                foreach (var stmt in block.Statements)
                {
                    if (stmt is Binding.BoundNodes.BoundReturnExpr { Value: { StaticType: var retType } }
                        && retType != typeof(object))
                        return retType;
                }
            }

            return bound.StaticType;
        }
        catch
        {
            return null;
        }
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

}
