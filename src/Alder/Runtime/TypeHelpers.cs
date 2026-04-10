using Alder.Runtime.Collections;

namespace Alder.Runtime;

internal static partial class TypeHelpers
{
    /// <summary>
    /// ECMA-334 §10.2.3: Implicit numeric conversions.
    /// "There are no predefined implicit conversions to the char type, so values of the
    /// other integral types do not automatically convert to the char type." (§10.2.3)
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
    /// ECMA-334 §10.2: Checks if sourceType can be implicitly converted to targetType.
    /// Handles numeric, nullable, reference, boxing, tuple, and user-defined conversions.
    /// </summary>
    public static bool CanImplicitlyConvert(Type sourceType, Type targetType)
    {
        // §10.4.2: Standard implicit conversions (identity, numeric, nullable, reference, boxing)
        if (IsStandardImplicitConversion(sourceType, targetType))
            return true;

        // §10.2.13: Implicit tuple conversions (element-wise)
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

        // §10.5.4: User-defined implicit conversions
        if (HasUserDefinedImplicitConversion(sourceType, targetType))
            return true;

        return false;
    }

    /// <summary>
    /// ECMA-334 §10.4.2: Standard implicit conversions (the pre-defined conversions that can
    /// occur as part of a user-defined conversion). Does NOT include user-defined conversions,
    /// preventing recursion when called from TryResolveUserDefinedConversion.
    /// </summary>
    internal static bool IsStandardImplicitConversion(Type sourceType, Type targetType)
    {
        // §10.2.2: Identity
        if (sourceType == targetType)
            return true;

        // §10.2.6: Implicit nullable conversions
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

        // §10.2.8: Implicit reference conversions and §10.2.9: Boxing conversions
        if (targetType.IsAssignableFrom(sourceType))
            return true;

        // §10.2.3: Implicit numeric conversions
        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(targetType))
            return true;

        return false;
    }

    /// <summary>
    /// ECMA-334 §12.6.4.7: T1 is a better conversion target than T2 if an implicit conversion
    /// from T1 to T2 exists and no implicit conversion from T2 to T1 exists, or if T1 is a
    /// signed integral type and T2 is an unsigned integral type per the spec's preference table.
    /// Returns positive if T1 is better, negative if T2 is better, zero if neither.
    /// </summary>
    public static int CompareBetterConversionTarget(Type t1, Type t2)
    {
        if (t1 == t2)
            return 0;

        var t1ToT2 = CanImplicitlyConvert(t1, t2);
        var t2ToT1 = CanImplicitlyConvert(t2, t1);

        if (t1ToT2 && !t2ToT1) return 1;
        if (t2ToT1 && !t1ToT2) return -1;

        // §12.6.4.7 rule 4: signed integral preferred over unsigned
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
    /// Returns the ECMA-334 binary numeric promotion type for arithmetic operands,
    /// or null when either operand is non-arithmetic.
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
