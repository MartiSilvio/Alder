using System.Diagnostics.CodeAnalysis;

namespace Alder.Runtime;

internal static partial class TypeHelpers
{
    /// <summary>
    /// ECMA-334 §10.5.5: Resolves user-defined explicit conversions.
    /// Searches source + target types for op_Explicit and op_Implicit operators,
    /// then selects the most specific source type (Sx) and target type (Tx).
    /// </summary>
    private static bool TryResolveUserDefinedConversion(
        Type sourceType,
        Type targetType,
        [NotNullWhen(true)] out MethodInfo? conversionMethod)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        conversionMethod = null;

        // §10.5.3: Search source type + base classes, and target type + base classes
        var searchTypes = sourceType == targetType
            ? new[] { sourceType }
            : new[] { sourceType, targetType };

        var candidates = new List<MethodInfo>();

        foreach (var declaringType in searchTypes)
        {
            foreach (var method in ReflectionRuntime.GetMethods(declaringType, flags))
            {
                if (method.Name is not ("op_Explicit" or "op_Implicit"))
                    continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 1)
                    continue;

                var paramType = parameters[0].ParameterType;
                var returnType = method.ReturnType;

                // §10.5.5: operator applicable if standard conversion (§10.4.2) exists
                // from sourceType -> paramType OR paramType -> sourceType (encompassing or encompassed)
                var sourceApplicable = IsStandardImplicitConversion(sourceType, paramType)
                    || IsStandardImplicitConversion(paramType, sourceType);

                // §10.5.5: standard conversion from returnType -> targetType or targetType -> returnType
                var targetApplicable = IsStandardImplicitConversion(returnType, targetType)
                    || IsStandardImplicitConversion(targetType, returnType);

                if (sourceApplicable && targetApplicable)
                    candidates.Add(method);
            }
        }

        if (candidates.Count == 0)
            return false;

        if (candidates.Count == 1)
        {
            conversionMethod = candidates[0];
            return true;
        }

        // §10.5.4/§10.5.5: Find most specific source type Sx
        conversionMethod = SelectMostSpecific(candidates, sourceType, targetType);
        return conversionMethod != null;
    }

    private static MethodInfo? SelectMostSpecific(List<MethodInfo> candidates, Type sourceType, Type targetType)
    {
        // Prefer exact source type match
        var exactSource = candidates.Where(m => m.GetParameters()[0].ParameterType == sourceType).ToList();
        if (exactSource.Count > 0)
            candidates = exactSource;

        // Prefer exact target type match
        var exactTarget = candidates.Where(m => m.ReturnType == targetType).ToList();
        if (exactTarget.Count > 0)
            candidates = exactTarget;

        if (candidates.Count == 1)
            return candidates[0];

        // §10.5.4: Most-encompassed source type (the one that all other
        // candidate source types can convert TO)
        MethodInfo? best = null;
        foreach (var candidate in candidates)
        {
            var cParamType = candidate.GetParameters()[0].ParameterType;
            var cReturnType = candidate.ReturnType;
            var isMostSpecific = true;

            foreach (var other in candidates)
            {
                if (ReferenceEquals(candidate, other))
                    continue;

                var oParamType = other.GetParameters()[0].ParameterType;
                var oReturnType = other.ReturnType;

                // Source: candidate is more specific if other's param encompasses candidate's param
                if (cParamType != oParamType && !CanImplicitlyConvert(cParamType, oParamType))
                {
                    isMostSpecific = false;
                    break;
                }

                // Target: candidate is more specific if candidate's return encompasses other's return
                if (cReturnType != oReturnType && !CanImplicitlyConvert(oReturnType, cReturnType))
                {
                    isMostSpecific = false;
                    break;
                }
            }

            if (isMostSpecific)
            {
                if (best != null)
                    return null; // Ambiguous
                best = candidate;
            }
        }

        return best;
    }

    internal static bool HasUserDefinedImplicitConversion(Type sourceType, Type targetType)
    {
        return TryResolveUserDefinedImplicitConversion(sourceType, targetType, out _);
    }

    private static bool TryResolveUserDefinedImplicitConversion(
        Type sourceType,
        Type targetType,
        [NotNullWhen(true)] out MethodInfo? conversionMethod)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        conversionMethod = null;

        var searchTypes = sourceType == targetType
            ? new[] { sourceType }
            : new[] { sourceType, targetType };

        List<MethodInfo>? candidates = null;

        foreach (var declaringType in searchTypes)
        {
            foreach (var method in ReflectionRuntime.GetMethods(declaringType, flags))
            {
                if (method.Name != "op_Implicit")
                    continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 1)
                    continue;

                var paramType = parameters[0].ParameterType;
                var returnType = method.ReturnType;

                // §10.5.4: Standard implicit conversion must exist from sourceType to paramType
                // AND from returnType to targetType (unidirectional, not bidirectional)
                if (IsStandardImplicitConversion(sourceType, paramType) &&
                    IsStandardImplicitConversion(returnType, targetType))
                {
                    candidates ??= new List<MethodInfo>();
                    candidates.Add(method);
                }
            }
        }

        if (candidates == null)
            return false;

        if (candidates.Count == 1)
        {
            conversionMethod = candidates[0];
            return true;
        }

        conversionMethod = SelectMostSpecific(candidates, sourceType, targetType);
        return conversionMethod != null;
    }

    internal static bool TryApplyUserDefinedImplicitConversion(object value, Type targetType, out object? converted)
    {
        converted = null;
        var sourceType = value.GetType();
        if (!TryResolveUserDefinedImplicitConversion(sourceType, targetType, out var method))
            return false;
        converted = method.Invoke(null, [value]);
        return true;
    }
}
