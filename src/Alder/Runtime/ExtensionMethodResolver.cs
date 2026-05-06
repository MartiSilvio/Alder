using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Alder.Diagnostics;
using Alder.Runtime.Collections;
using Alder.Runtime.OverloadResolution;

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

    private readonly struct InvocationCacheKey : IEquatable<InvocationCacheKey>
    {
        public readonly Type ExtensionType;
        public readonly Type TargetType;
        public readonly string MethodNameKey;
        public readonly bool IsCaseSensitive;
        public readonly string TypeArgSignature;
        public readonly ImmutableArray<InvocationArgumentShape> ArgumentShapes;

        public InvocationCacheKey(
            Type extensionType, Type targetType, string methodNameKey,
            bool isCaseSensitive, string typeArgSignature,
            ImmutableArray<InvocationArgumentShape> argumentShapes)
        {
            ExtensionType = extensionType;
            TargetType = targetType;
            MethodNameKey = methodNameKey;
            IsCaseSensitive = isCaseSensitive;
            TypeArgSignature = typeArgSignature;
            ArgumentShapes = argumentShapes;
        }

        public bool Equals(InvocationCacheKey other)
        {
            return ExtensionType == other.ExtensionType &&
                   TargetType == other.TargetType &&
                   MethodNameKey == other.MethodNameKey &&
                   IsCaseSensitive == other.IsCaseSensitive &&
                   TypeArgSignature == other.TypeArgSignature &&
                   ArgumentShapes.SequenceEqual(other.ArgumentShapes);
        }

        public override bool Equals(object? obj) => obj is InvocationCacheKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (ExtensionType?.GetHashCode() ?? 0) * 397;
                hash = (hash ^ (TargetType?.GetHashCode() ?? 0)) * 397;
                hash = (hash ^ (MethodNameKey?.GetHashCode() ?? 0)) * 397;
                hash = (hash ^ IsCaseSensitive.GetHashCode()) * 397;
                hash = (hash ^ (TypeArgSignature?.GetHashCode() ?? 0)) * 397;
                foreach (var shape in ArgumentShapes)
                    hash = (hash ^ shape.GetHashCode()) * 397;
                return hash;
            }
        }
    }

    private static readonly ConcurrentDictionary<(Type ExtensionType, string MethodNameKey, bool IsCaseSensitive), MethodInfo[]> ExtensionMethodsByNameCache = new();
    private static readonly ConcurrentDictionary<(Type ExtensionType, string MethodNameKey, bool IsCaseSensitive, int InvocationArgCount), MethodInfo[]> ExtensionMethodsByArityCache = new();
    private static readonly BoundedConcurrentCache<InvocationCacheKey, ResolvedCall?> ResolvedCallCache = new(4096);

    private static string GetMethodNameCacheKey(string methodName, bool isCaseSensitive) =>
        isCaseSensitive ? methodName : methodName.ToUpperInvariant();

    internal static (bool Success, object? Value) TryInvoke(
        object target,
        string methodName,
        object?[] args,
        AlderContext context,
        IReadOnlyList<string>? typeArgs = null,
        CancellationToken ct = default)
    {
        if (!MethodDispatchCache.DynamicCodeSupported)
            return (false, null);

        var extensionTypes = context.ExtensionTypes;
        if (extensionTypes.IsDefaultOrEmpty)
            return (false, null);

        var targetType = target.GetType();
        var invocationArgs = new object?[args.Length + 1];
        invocationArgs[0] = target;
        Array.Copy(args, 0, invocationArgs, 1, args.Length);
        invocationArgs = TryResolveLambdaArgs(invocationArgs, targetType, context) ?? invocationArgs;

        foreach (var extType in extensionTypes)
        {
            var result = TryInvokeFromType(
                targetType, methodName, invocationArgs, extType,
                context.Config.IsCaseSensitive, typeArgs, context, ct);
            if (result.Success)
                return result;
        }

        return (false, null);
    }

    private static (bool Success, object? Value) TryInvokeFromType(
        Type targetType,
        string methodName,
        object?[] invocationArgs,
        Type extensionType,
        bool isCaseSensitive,
        IReadOnlyList<string>? typeArgs = null,
        AlderContext? runtimeContext = null,
        CancellationToken ct = default)
    {
        var methodNameKey = GetMethodNameCacheKey(methodName, isCaseSensitive);

        InvocationCacheKey? invocationCacheKey = null;
        if (TryCreateInvocationCacheKey(
                extensionType, targetType, methodNameKey, invocationArgs.AsSpan(1),
                isCaseSensitive, typeArgs, out var cacheKey))
        {
            invocationCacheKey = cacheKey;
            if (ResolvedCallCache.TryGetValue(cacheKey, out var cached))
            {
                if (cached == null)
                    return (false, null);

                return InvokeWithResolved(cached.Value, invocationArgs, runtimeContext, ct);
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
                ResolvedCallCache.TryAdd(missingCacheKey, null);
            return (false, null);
        }

        if (invocationCacheKey is { } resolvedCacheKey)
            ResolvedCallCache.TryAdd(resolvedCacheKey, resolved);

        return InvokeWithResolved(resolved, invocationArgs, runtimeContext, ct);
    }

    private static (bool Success, object? Value) InvokeWithResolved(
        ResolvedCall resolved,
        object?[] args,
        AlderContext? runtimeContext,
        CancellationToken ct)
    {
        if (runtimeContext == null)
            throw new InvalidOperationException("Resolved extension invocation requires an AlderContext.");

        var result = MethodInvoker.InvokeResolvedCall(resolved, null, args, runtimeContext, ct);
        return (true, result);
    }

    private static MethodInfo[] GetExtensionMethodsByName(
        Type extensionType,
        string methodName,
        bool isCaseSensitive)
    {
        var methodNameKey = GetMethodNameCacheKey(methodName, isCaseSensitive);
        return ExtensionMethodsByNameCache.GetOrAdd(
            (extensionType, methodNameKey, isCaseSensitive),
            static key =>
            {
                var comparison = key.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                return RuntimeTypeIntrospection
                    .GetMethods(key.ExtensionType, BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name.Equals(key.MethodNameKey, comparison) &&
                                m.IsDefined(typeof(ExtensionAttribute), false))
                    .ToArray();
            });
    }

    internal static MethodInfo[] GetExtensionMethodsForArity(
        Type extensionType,
        string methodName,
        bool isCaseSensitive,
        int invocationArgCount)
    {
        var methodNameKey = GetMethodNameCacheKey(methodName, isCaseSensitive);
        return ExtensionMethodsByArityCache.GetOrAdd(
            (extensionType, methodNameKey, isCaseSensitive, invocationArgCount),
            static key =>
            {
                var methods = GetExtensionMethodsByName(key.ExtensionType, key.MethodNameKey, key.IsCaseSensitive);
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
        string methodNameKey,
        ReadOnlySpan<object?> args,
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

    private static object?[]? TryResolveLambdaArgs(object?[] invocationArgs, Type targetType, AlderContext context)
    {
        var elementType = TypeHelpers.GetEnumerableElementType(targetType);
        if (elementType == null)
            return null;

        var hasConvertible = false;
        for (var i = 1; i < invocationArgs.Length; i++)
        {
            if (invocationArgs[i] is LambdaValue or CompiledLambdaValue or MethodRef or ModuleMethodRef)
            {
                hasConvertible = true;
                break;
            }
        }

        if (!hasConvertible)
            return null;

        var resolved = new object?[invocationArgs.Length];
        resolved[0] = invocationArgs[0];

        var inputTypes = new[] { CreateEnumerableInputType(invocationArgs[0], elementType) };
        for (var i = 1; i < invocationArgs.Length; i++)
        {
            var arg = invocationArgs[i];

            if (arg is not (LambdaValue or CompiledLambdaValue or MethodRef or ModuleMethodRef))
            {
                resolved[i] = arg;
                continue;
            }

            var returnType = InferLambdaReturnType(arg, inputTypes, context);
            if (returnType == null || returnType == typeof(object))
            {
                resolved[i] = arg;
                continue;
            }

            if (!RuntimeGenericClosure.TryCloseType(
                    typeof(Func<,>),
                    [elementType, returnType],
                    context.Config.RootedDelegateTypes,
                    out var delegateType))
            {
                resolved[i] = arg;
                continue;
            }

            resolved[i] = LambdaDelegateConverter.TryConvert(arg, delegateType!) ?? arg;
        }

        return resolved;
    }

    private static Binding.BoundType CreateEnumerableInputType(object? enumerable, Type elementType)
    {
        if (enumerable is not IEnumerable sequence)
            return new Binding.BoundType(elementType);

        var enumerator = sequence.GetEnumerator();
        try
        {
            if (!enumerator.MoveNext())
                return new Binding.BoundType(elementType);

            if (enumerator.Current is StructuralObjectValue structural)
            {
                var members = ImmutableDictionary.CreateBuilder<string, Type>();
                foreach (var member in structural.TypeInfo.Members)
                    members[member.Name] = member.Type;
                return new Binding.BoundStructuralType(elementType, members.ToImmutable());
            }

            if (enumerator.Current is IDictionary<string, object?> dict)
            {
                var members = ImmutableDictionary.CreateBuilder<string, Type>();
                foreach (var (name, value) in dict)
                    members[name] = value?.GetType() ?? typeof(object);
                return new Binding.BoundStructuralType(elementType, members.ToImmutable());
            }

            return new Binding.BoundType(elementType);
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
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
            case Binding.BoundNodes.BoundLambdaExpr boundLambda:
                parameters = boundLambda.Parameters;
                body = boundLambda.Body;
                break;
            case MethodRef or ModuleMethodRef:
            {
                var paramTypes = new Type[inputTypes.Length];
                for (var i = 0; i < inputTypes.Length; i++)
                    paramTypes[i] = inputTypes[i].ClrType;
                return OverloadResolver.TryInferMethodGroupReturnType(arg, paramTypes);
            }
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
        catch (Exception ex) when (ex is AlderException or InvalidOperationException or ArgumentException or InsufficientExecutionStackException)
        {
            return null;
        }
    }
}
