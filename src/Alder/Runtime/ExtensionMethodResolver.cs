using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Alder.Diagnostics;

namespace Alder.Runtime;

internal static class ExtensionMethodResolver
{
    private enum InvocationArgumentKind : byte
    {
        Null,
        RuntimeType
    }

    private readonly record struct InvocationArgumentShape(
        InvocationArgumentKind Kind,
        Type? RuntimeType);

    private readonly record struct InvocationCacheKey(
        Type ExtensionType,
        Type TargetType,
        string MethodNameKey,
        bool IsCaseSensitive,
        string TypeArgSignature,
        ImmutableArray<InvocationArgumentShape> ArgumentShapes);

    private static readonly ConcurrentDictionary<(Type ExtensionType, string MethodNameKey, bool IsCaseSensitive), MethodInfo[]> ExtensionMethodsByNameCache = new();
    private static readonly ConcurrentDictionary<(Type ExtensionType, string MethodNameKey, bool IsCaseSensitive, int InvocationArgCount), MethodInfo[]> ExtensionMethodsByArityCache = new();
    private static readonly ConcurrentDictionary<InvocationCacheKey, ResolvedCall?> ResolvedCallCache = new();
    private static readonly ConcurrentQueue<InvocationCacheKey> _resolvedCallInsertionOrder = new();
    private const int MaxResolvedCallCacheSize = 4096;

    private static void CacheResolvedCall(InvocationCacheKey key, ResolvedCall? resolved)
    {
        if (ResolvedCallCache.TryAdd(key, resolved))
        {
            _resolvedCallInsertionOrder.Enqueue(key);
            while (ResolvedCallCache.Count > MaxResolvedCallCacheSize &&
                   _resolvedCallInsertionOrder.TryDequeue(out var oldest))
            {
                ResolvedCallCache.TryRemove(oldest, out _);
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

    internal static (bool Success, object? Value) TryInvokeFromType(
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
                extensionType, targetType, methodName, args,
                isCaseSensitive, typeArgs, out var cacheKey))
        {
            invocationCacheKey = cacheKey;
            if (ResolvedCallCache.TryGetValue(cacheKey, out var cached))
            {
                if (cached == null)
                    return (false, null);

                return InvokeWithResolved(cached.Value, invocationArgs, ct);
            }
        }

        var methods = GetExtensionMethodsForArity(extensionType, methodName, isCaseSensitive, invocationArgs.Length);
        var descriptors = ArgumentDescriptor.FromArgs(invocationArgs);

        if (!OverloadResolver.TryResolveExtension(
                methods, targetType, descriptors, runtimeContext,
                out var resolved, out var ambiguous, typeArgs, ct))
        {
            if (ambiguous)
                throw new AlderException(DiagnosticDescriptors.AmbiguousMethodInvocation, methodName);

            if (invocationCacheKey is { } missingCacheKey)
                CacheResolvedCall(missingCacheKey, null);
            return (false, null);
        }

        if (invocationCacheKey is { } resolvedCacheKey)
            CacheResolvedCall(resolvedCacheKey, resolved);

        return InvokeWithResolved(resolved, invocationArgs, ct);
    }

    private static (bool Success, object? Value) InvokeWithResolved(
        ResolvedCall resolved,
        object?[] args,
        CancellationToken ct)
    {
        var parameters = MethodDispatchCache.GetParameters(resolved.Method);
        var prepared = ArgumentPreparer.Prepare(resolved, args, parameters, ct);
        var result = MethodInvoker.InvokeMethodCore(resolved.Method, null, prepared);
        return (true, result);
    }

    private static MethodInfo[] GetExtensionMethodsByNormalizedName(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type extensionType,
        string methodNameKey, bool isCaseSensitive)
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
                case LambdaValue:
                case CompiledLambdaValue:
                    key = default;
                    return false;
                case null:
                    argumentShapes.Add(new InvocationArgumentShape(InvocationArgumentKind.Null, null));
                    break;
                default:
                    argumentShapes.Add(new InvocationArgumentShape(InvocationArgumentKind.RuntimeType, arg.GetType()));
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

    internal static Type? InferLambdaReturnType(object? arg, Binding.BoundType[] inputTypes, AlderContext? runtimeContext)
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

            if (bound is Binding.BoundNodes.BoundBlockExpr block)
            {
                foreach (var stmt in block.Statements)
                {
                    if (stmt is Binding.BoundNodes.BoundReturnExpr { Value: { StaticType: var retType } }
                        && retType.ClrType != typeof(object))
                        return retType.ClrType;
                }
            }

            return bound.StaticType.ClrType;
        }
        catch
        {
            return null;
        }
    }
}
