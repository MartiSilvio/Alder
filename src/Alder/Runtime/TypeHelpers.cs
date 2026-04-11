using Alder.Runtime.Collections;

namespace Alder.Runtime;

internal static partial class TypeHelpers
{
    /// <summary>
    /// The predefined implicit numeric conversion graph from ECMA-334 §10.2.3.
    /// Notably, no numeric type implicitly converts to <see cref="char"/>.
    /// </summary>
    private static readonly FixedDictionary<Type, FixedSet<Type>> ImplicitConversions = FixedDictionary<Type, FixedSet<Type>>.Create(
        new Dictionary<Type, HashSet<Type>>
        {
            [typeof(sbyte)] = [typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
            [typeof(byte)] = [typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
            [typeof(short)] = [typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
            [typeof(ushort)] = [typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
            [typeof(int)] = [typeof(long), typeof(float), typeof(double), typeof(decimal)],
            [typeof(uint)] = [typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
            [typeof(long)] = [typeof(float), typeof(double), typeof(decimal)],
            [typeof(ulong)] = [typeof(float), typeof(double), typeof(decimal)],
            [typeof(float)] = [typeof(double)],
            [typeof(char)] = [typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        },
        kvp => kvp.Key,
        kvp => FixedSet<Type>.Create(kvp.Value));

    /// <summary>
    /// Determines whether <paramref name="sourceType"/> can be implicitly converted to <paramref name="targetType"/>.
    /// This includes standard conversions, tuple conversions, and user-defined implicit operators.
    /// </summary>
    public static bool CanImplicitlyConvert(Type sourceType, Type targetType)
    {
        // Standard implicit conversions are checked first because user-defined conversions build on top of them.
        if (IsStandardImplicitConversion(sourceType, targetType))
            return true;

        // ECMA tuple conversions are element-wise and recurse through the same implicit-conversion rules.
        if (IsTupleType(sourceType) && IsTupleType(targetType))
        {
            var sourceArgs = sourceType.GetGenericArguments();
            var targetArgs = targetType.GetGenericArguments();
            if (sourceArgs.Length != targetArgs.Length)
                return false;
            for (var i = 0; i < sourceArgs.Length; i++)
            {
                if (!CanImplicitlyConvert(sourceArgs[i], targetArgs[i]))
                    return false;
            }
            return true;
        }

        // User-defined implicit operators are considered only after the standard conversion lattice is exhausted.
        if (HasUserDefinedImplicitConversion(sourceType, targetType))
            return true;

        return false;
    }

    /// <summary>
    /// Determines whether a standard implicit conversion exists.
    /// User-defined conversions are intentionally excluded so this helper can be used safely while resolving user-defined operators.
    /// </summary>
    internal static bool IsStandardImplicitConversion(Type sourceType, Type targetType)
    {
        // ECMA identity conversion.
        if (sourceType == targetType)
            return true;

        // Nullable lifting reuses the same predefined conversion graph for the underlying types.
        var underlyingTarget = Nullable.GetUnderlyingType(targetType);
        if (underlyingTarget != null)
        {
            if (sourceType == underlyingTarget)
                return true;

            if (ImplicitConversions.TryGetValue(sourceType, out var nullableTargets) && nullableTargets.Contains(underlyingTarget))
                return true;

            var underlyingSource = Nullable.GetUnderlyingType(sourceType);
            if (underlyingSource != null)
            {
                if (underlyingSource == underlyingTarget)
                    return true;
                if (ImplicitConversions.TryGetValue(underlyingSource, out var liftedTargets) && liftedTargets.Contains(underlyingTarget))
                    return true;
            }
        }

        // Reference and boxing conversions are delegated to the CLR assignability relationship.
        if (targetType.IsAssignableFrom(sourceType))
            return true;

        // Numeric conversions are table-driven from the ECMA graph above.
        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(targetType))
            return true;

        return false;
    }

    /// <summary>
    /// Compares two candidate conversion targets using the better-conversion rules from ECMA-334 §12.6.4.7.
    /// </summary>
    public static int CompareBetterConversionTarget(Type t1, Type t2)
    {
        if (t1 == t2)
            return 0;

        var t1ToT2 = CanImplicitlyConvert(t1, t2);
        var t2ToT1 = CanImplicitlyConvert(t2, t1);

        if (t1ToT2 && !t2ToT1) return 1;
        if (t2ToT1 && !t1ToT2) return -1;

        // The specification prefers certain signed integral targets over unsigned ones when neither direction converts implicitly.
        var s1 = Nullable.GetUnderlyingType(t1) ?? t1;
        var s2 = Nullable.GetUnderlyingType(t2) ?? t2;
        if (IsSignedPreferredOver(s1, s2)) return 1;
        if (IsSignedPreferredOver(s2, s1)) return -1;

        return 0;
    }

    private static bool IsSignedPreferredOver(Type signed, Type unsigned) =>
        (signed == typeof(sbyte) && unsigned is { } u && (u == typeof(byte) || u == typeof(ushort) || u == typeof(uint) || u == typeof(ulong))) ||
        (signed == typeof(short) && unsigned is { } u2 && (u2 == typeof(ushort) || u2 == typeof(uint) || u2 == typeof(ulong))) ||
        (signed == typeof(int) && unsigned is { } u3 && (u3 == typeof(uint) || u3 == typeof(ulong))) ||
        (signed == typeof(long) && unsigned == typeof(ulong));

    public static bool CanAssignOrImplicitlyConvert(Type sourceType, Type targetType)
    {
        if (targetType.IsAssignableFrom(sourceType))
            return true;

        var sourceUnderlying = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        var targetUnderlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (targetUnderlying.IsAssignableFrom(sourceUnderlying))
            return true;

        return CanImplicitlyConvert(sourceType, targetType) ||
               CanImplicitlyConvert(sourceUnderlying, targetUnderlying);
    }

    /// <summary>
    /// Returns the ECMA binary numeric promotion result type for arithmetic operands.
    /// Returns <c>null</c> when the operands do not participate in arithmetic promotion.
    /// </summary>
    public static Type? TryGetBinaryNumericPromotionType(Type leftType, Type rightType)
    {
        if (!IsArithmetic(leftType) || !IsArithmetic(rightType))
            return null;

        // ECMA-334: decimal cannot be combined with float or double (CS0019)
        if (leftType == typeof(decimal) && (rightType == typeof(float) || rightType == typeof(double)))
            return null;
        if (rightType == typeof(decimal) && (leftType == typeof(float) || leftType == typeof(double)))
            return null;

        return NumericDispatch.TryGetResultType(leftType, rightType);
    }
}
