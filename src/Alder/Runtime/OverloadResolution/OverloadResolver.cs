using System.Collections.Immutable;
using System.Reflection;

namespace Alder.Runtime;

/// <summary>ECMA-334 §12.6.4 overload resolution.</summary>
internal static class OverloadResolver
{
    public static bool TryResolve(
        MethodInfo[] methods,
        ReadOnlySpan<ArgumentDescriptor> args,
        AlderContext? context,
        out ResolvedCall result,
        out bool ambiguous,
        IReadOnlyList<string>? typeArgs = null,
        CancellationToken ct = default)
    {
        result = default;
        ambiguous = false;

        var candidates = BuildCandidates(methods, args, context, typeArgs);
        if (candidates.Count == 0)
            return false;

        if (candidates.Count == 1)
        {
            result = candidates[0].ToResolvedCall();
            return true;
        }

        FilterMostDerived(candidates);
        if (candidates.Count == 1)
        {
            result = candidates[0].ToResolvedCall();
            return true;
        }

        return SelectBest(candidates, args, out result, out ambiguous);
    }

    public static bool TryResolveExtension(
        IReadOnlyList<MethodInfo> extensionMethods,
        Type targetType,
        ReadOnlySpan<ArgumentDescriptor> receiverAndArgs,
        AlderContext? context,
        out ResolvedCall result,
        out bool ambiguous,
        IReadOnlyList<string>? typeArgs = null,
        CancellationToken ct = default)
    {
        result = default;
        ambiguous = false;

        var candidates = BuildExtensionCandidates(
            extensionMethods, targetType, receiverAndArgs, context, typeArgs);
        if (candidates.Count == 0)
            return false;

        if (candidates.Count == 1)
        {
            result = candidates[0].ToResolvedCall();
            return true;
        }

        return SelectBest(candidates, receiverAndArgs, out result, out ambiguous);
    }

    private static List<MethodCandidate> BuildCandidates(
        MethodInfo[] methods,
        ReadOnlySpan<ArgumentDescriptor> args,
        AlderContext? context,
        IReadOnlyList<string>? typeArgs)
    {
        var candidates = new List<MethodCandidate>(methods.Length);

        foreach (var method in methods)
        {
            if (TryBuildCandidate(method, args, context, typeArgs, out var candidate))
                candidates.Add(candidate);
        }

        return candidates;
    }

    private static List<MethodCandidate> BuildExtensionCandidates(
        IReadOnlyList<MethodInfo> extensionMethods,
        Type targetType,
        ReadOnlySpan<ArgumentDescriptor> receiverAndArgs,
        AlderContext? context,
        IReadOnlyList<string>? typeArgs)
    {
        var candidates = new List<MethodCandidate>(extensionMethods.Count);

        foreach (var method in extensionMethods)
        {
            var concreteMethod = TryCloseGenericMethod(method, receiverAndArgs, context, typeArgs);
            if (concreteMethod == null)
                continue;

            var concreteParams = MethodDispatchCache.GetParameters(concreteMethod);
            if (concreteParams.Length == 0)
                continue;

            if (!IsExtensionCompatible(targetType, concreteParams[0].ParameterType))
                continue;

            if (!ApplicabilityChecker.IsApplicable(
                    concreteParams, receiverAndArgs,
                    out var form, out var argMap, out var conversions))
                continue;

            var lambdaReturnTypes = PreComputeLambdaReturnTypes(
                receiverAndArgs, argMap, concreteParams, context);
            var genericDef = method.IsGenericMethodDefinition ? method : null;
            candidates.Add(new MethodCandidate(
                concreteMethod, concreteParams, form, argMap, conversions, genericDef, lambdaReturnTypes));
        }

        return candidates;
    }

    private static bool TryBuildCandidate(
        MethodInfo method,
        ReadOnlySpan<ArgumentDescriptor> args,
        AlderContext? context,
        IReadOnlyList<string>? typeArgs,
        out MethodCandidate candidate)
    {
        candidate = default;

        var concreteMethod = TryCloseGenericMethod(method, args, context, typeArgs);
        if (concreteMethod == null)
            return false;

        var parameters = MethodDispatchCache.GetParameters(concreteMethod);

        if (!ApplicabilityChecker.IsApplicable(
                parameters, args,
                out var form, out var argMap, out var conversions))
            return false;

        var lambdaReturnTypes = PreComputeLambdaReturnTypes(args, argMap, parameters, context);

        var genericDef = method.IsGenericMethodDefinition ? method : null;
        candidate = new MethodCandidate(
            concreteMethod, parameters, form, argMap, conversions, genericDef, lambdaReturnTypes);
        return true;
    }

    private static Type?[]? PreComputeLambdaReturnTypes(
        ReadOnlySpan<ArgumentDescriptor> args,
        ArgumentParameterMap argMap,
        ParameterInfo[] parameters,
        AlderContext? context)
    {
        if (context == null)
            return null;

        Type?[]? result = null;
        var sources = argMap.Sources;
        var elementMemberTypes = TryExtractElementMemberTypes(args);

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Kind != ArgumentKind.Lambda)
                continue;

            var paramIdx = FindParameterForArgument(sources, i);
            if (paramIdx < 0 || paramIdx >= parameters.Length)
                continue;

            var delegateType = parameters[paramIdx].ParameterType;
            var invoke = delegateType.GetMethod("Invoke");
            if (invoke == null || invoke.ReturnType == typeof(void))
                continue;

            var invokeParams = invoke.GetParameters();
            var inputTypes = new Binding.BoundType[invokeParams.Length];
            for (var j = 0; j < inputTypes.Length; j++)
            {
                var paramType = invokeParams[j].ParameterType;
                inputTypes[j] = elementMemberTypes != null && typeof(IDictionary<string, object?>).IsAssignableFrom(paramType)
                    ? new Binding.BoundType(paramType, elementMemberTypes)
                    : new Binding.BoundType(paramType);
            }

            result ??= new Type?[args.Length];
            result[i] = ExtensionMethodResolver.InferLambdaReturnType(
                args[i].RuntimeValue, inputTypes, context);
        }

        return result;
    }

    private static System.Collections.Immutable.ImmutableDictionary<string, Type>? TryExtractElementMemberTypes(
        ReadOnlySpan<ArgumentDescriptor> args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Kind != ArgumentKind.Value || args[i].RuntimeValue == null)
                continue;

            if (args[i].RuntimeValue is not System.Collections.IEnumerable enumerable)
                continue;

            var enumerator = enumerable.GetEnumerator();
            try
            {
                if (!enumerator.MoveNext() || enumerator.Current is not IDictionary<string, object?> dict)
                    return null;

                var builder = System.Collections.Immutable.ImmutableDictionary.CreateBuilder<string, Type>();
                foreach (var kvp in dict)
                    builder[kvp.Key] = kvp.Value?.GetType() ?? typeof(object);
                return builder.ToImmutable();
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }

        return null;
    }

    private static int FindParameterForArgument(ImmutableArray<ParameterSource> sources, int argIndex)
    {
        for (var i = 0; i < sources.Length; i++)
        {
            if (sources[i].Kind == ParameterSourceKind.Argument && sources[i].ArgumentIndex == argIndex)
                return i;
        }
        return -1;
    }

    private static MethodInfo? TryCloseGenericMethod(
        MethodInfo method,
        ReadOnlySpan<ArgumentDescriptor> args,
        AlderContext? context,
        IReadOnlyList<string>? typeArgs)
    {
        if (!method.ContainsGenericParameters)
            return method;

        if (typeArgs is { Count: > 0 })
            return MethodInvoker.TryMakeConcreteMethodWithTypeArgs(
                method, typeArgs, context?.TypeResolver);

        return InferAndClose(method, args, context);
    }

    private static MethodInfo? InferAndClose(
        MethodInfo genericMethod,
        ReadOnlySpan<ArgumentDescriptor> args,
        AlderContext? context)
    {
        var argTypes = new Type?[args.Length];
        object?[]? lambdaArgs = null;

        for (var i = 0; i < args.Length; i++)
        {
            argTypes[i] = args[i].StaticType;

            if (args[i].Kind == ArgumentKind.Lambda)
            {
                lambdaArgs ??= new object?[args.Length];
                lambdaArgs[i] = args[i].RuntimeValue;
            }
        }

        var inferred = TypeInference.Infer(genericMethod, argTypes, lambdaArgs, context);
        if (inferred == null)
            return null;

        return RuntimeGenericFactory.TryCloseGenericMethod(genericMethod, inferred, out var closed)
            ? closed
            : null;
    }

    private static void FilterMostDerived(List<MethodCandidate> candidates)
    {
        if (candidates.Count <= 1)
            return;

        for (var i = candidates.Count - 1; i >= 0; i--)
        {
            var declaringType = candidates[i].Method.DeclaringType;
            if (declaringType == null)
                continue;

            for (var j = 0; j < candidates.Count; j++)
            {
                if (i == j)
                    continue;

                var otherDeclaringType = candidates[j].Method.DeclaringType;
                if (otherDeclaringType == null)
                    continue;

                if (declaringType != otherDeclaringType &&
                    declaringType.IsAssignableFrom(otherDeclaringType))
                {
                    candidates.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private static bool SelectBest(
        List<MethodCandidate> candidates,
        ReadOnlySpan<ArgumentDescriptor> args,
        out ResolvedCall result,
        out bool ambiguous)
    {
        result = default;
        ambiguous = false;

        var best = candidates[0];
        var bestIsUnique = true;

        for (var i = 1; i < candidates.Count; i++)
        {
            var cmp = OverloadResolution.BetterFunctionMember(best, candidates[i], args);
            switch (cmp)
            {
                case BetterResult.Right:
                    best = candidates[i];
                    bestIsUnique = true;
                    break;
                case BetterResult.Neither:
                    bestIsUnique = false;
                    break;
            }
        }

        if (!bestIsUnique)
        {
            foreach (var candidate in candidates)
            {
                if (candidate.Method == best.Method)
                    continue;
                if (OverloadResolution.BetterFunctionMember(best, candidate, args) != BetterResult.Left)
                {
                    ambiguous = true;
                    return false;
                }
            }
        }

        result = best.ToResolvedCall();
        return true;
    }

    private static bool IsExtensionCompatible(Type sourceType, Type targetType)
    {
        if (targetType.IsAssignableFrom(sourceType))
            return true;

        foreach (var iface in ReflectionRuntime.GetInterfaces(sourceType))
        {
            if (targetType.IsAssignableFrom(iface))
                return true;

            if (!targetType.IsGenericType)
                continue;

            if (iface.IsGenericType &&
                iface.GetGenericTypeDefinition() == targetType.GetGenericTypeDefinition())
                return true;
        }

        return false;
    }
}
