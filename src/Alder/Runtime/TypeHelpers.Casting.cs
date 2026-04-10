using Alder.Diagnostics;

namespace Alder.Runtime;

internal static partial class TypeHelpers
{
    /// <summary>
    /// Performs an explicit cast with optional static type checking.
    /// When sourceStaticType is 'object', enforces C# unboxing semantics:
    /// you can only unbox to the exact boxed type.
    /// </summary>
    public static object? ExplicitCast(object? value, Type targetType, Type? sourceStaticType = null, bool isChecked = false)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var isNullable = Nullable.GetUnderlyingType(targetType) != null;

        if (value == null)
        {
            if (targetType.IsValueType && !isNullable)
                throw new AlderException(DiagnosticDescriptors.NullToNonNullable, targetType.Name);
            return null;
        }

        var runtimeType = value.GetType();

        // Same type - no conversion needed
        if (runtimeType == underlyingType || runtimeType == targetType)
            return value;

        // Handle reference types (string, object)
        if (underlyingType == typeof(string))
        {
            if (value is char c)
                return c.ToString();
            throw new AlderException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, "String");
        }

        if (underlyingType == typeof(object))
            return value;

        // Lambda-to-delegate conversion (works for both implicit and explicit casts)
        var delegateInstance = LambdaDelegateConverter.TryConvert(value, underlyingType);
        if (delegateInstance != null)
            return delegateInstance;

        // Reference type cast: check assignability for non-value-type targets
        if (!underlyingType.IsValueType && underlyingType != typeof(string))
        {
            if (underlyingType.IsAssignableFrom(runtimeType))
                return value;
            throw new AlderException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, underlyingType.Name);
        }

        // C# unboxing rule: when source static type is 'object', you can only unbox to the exact boxed type
        // (long)(object)42 fails because 42 is boxed as int, not long
        if (sourceStaticType == typeof(object) && underlyingType.IsValueType && runtimeType != underlyingType)
        {
            throw new AlderException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, underlyingType.Name);
        }

        // Numeric and char conversions
        try
        {
            if (underlyingType == typeof(char) && value is string { Length: 1 } s)
                return s[0];

            return RuntimeCast(value, runtimeType, underlyingType, isChecked);
        }
        catch (OverflowException) when (isChecked)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new AlderException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, targetType.Name);
        }
    }

    internal static object RuntimeCast(object value, Type sourceType, Type targetType, bool isChecked = false)
    {
        if (sourceType == targetType)
            return value;

        var source = sourceType.IsEnum ? Enum.GetUnderlyingType(sourceType) : sourceType;
        var target = targetType.IsEnum ? Enum.GetUnderlyingType(targetType) : targetType;

        if (IsNumericOrCharType(source) && IsNumericOrCharType(target))
        {
            var numericValue = sourceType.IsEnum
                ? Convert.ChangeType(value, source)
                : value;

            var converted = NumericCast(numericValue, source, target, isChecked);

            return targetType.IsEnum
                ? Enum.ToObject(targetType, converted)
                : converted;
        }

        if (TryResolveUserDefinedConversion(sourceType, targetType, out var conversionMethod))
            return conversionMethod.Invoke(null, [value])!;

        throw new AlderException(DiagnosticDescriptors.NoExplicitConversion, sourceType.Name, targetType.Name);
    }

    /// <summary>
    /// ECMA-334 §10.3.2: Explicit numeric conversions between numeric/char types.
    /// Handles checked/unchecked contexts per §12.8.19.
    /// </summary>
    private static object NumericCast(object value, Type sourceType, Type targetType, bool isChecked)
    {
        if (sourceType == targetType)
            return value;

        var targetCode = Type.GetTypeCode(targetType);

        // §8.3.6: char is a 16-bit unsigned integer; convert to ushort for arithmetic
        if (sourceType == typeof(char))
            return NumericCast((ushort)(char)value, typeof(ushort), targetType, isChecked);

        if (targetType == typeof(char))
        {
            var asUshort = NumericCast(value, sourceType, typeof(ushort), isChecked);
            return isChecked ? checked((char)(ushort)asUshort) : (char)(ushort)asUshort;
        }

        return NumericCastTable.Cast(value, targetCode, isChecked);
    }

    /// <summary>
    /// Converts a numeric value to a target type, handling char specially.
    /// System.Convert.ChangeType does not support char -> float/double/decimal directly,
    /// so we first convert char to ushort (its underlying numeric representation per ECMA-334 §8.3.6)
    /// before calling Convert.ChangeType.
    /// </summary>
    private static object ConvertNumeric(object value, Type sourceType, Type targetType)
    {
        // ECMA-334 §8.3.6: char is a 16-bit unsigned integer (same range as ushort)
        // Convert.ChangeType(char, float/double/decimal) throws InvalidCastException,
        // so convert char to its numeric value first.
        if (sourceType == typeof(char))
            return Convert.ChangeType((ushort)(char)value, targetType);

        return Convert.ChangeType(value, targetType);
    }

    public static object? ValidateAndCoerceType(Type targetType, object? value, string varName, bool isConstantExpression = true)
    {
        if (targetType == typeof(object))
            return value;

        var isNullable = Nullable.GetUnderlyingType(targetType) != null;
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value == null)
        {
            if (targetType.IsValueType && !isNullable)
                throw new AlderException(DiagnosticDescriptors.NullToNonNullable, targetType.Name);
            return null;
        }

        if (value is NamedTupleValue && IsTupleType(underlyingType))
            return value;

        var sourceType = value.GetType();

        if (sourceType == underlyingType || sourceType == targetType)
            return value;

        if (targetType.IsAssignableFrom(sourceType))
            return value;

        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(underlyingType))
            return ConvertNumeric(value, sourceType, underlyingType);

        // ECMA-334 §10.2.11: Implicit constant expression conversions
        if (isConstantExpression && sourceType == typeof(int) && value is int intValue && IsIntegerType(underlyingType) && !underlyingType.IsEnum)
        {
            try { return Convert.ChangeType(intValue, underlyingType); }
            catch (OverflowException)
            {
                throw new AlderException(
                    DiagnosticDescriptors.ConstantValueCannotConvert,
                    intValue,
                    underlyingType.Name);
            }
        }

        // §10.2.11: long -> ulong when non-negative
        if (isConstantExpression && sourceType == typeof(long) && value is long longValue && underlyingType == typeof(ulong))
        {
            if (longValue >= 0)
                return (ulong)longValue;
            throw new AlderException(DiagnosticDescriptors.ConstantValueCannotConvert, longValue, underlyingType.Name);
        }

        // §10.2.4/§10.3.3: Enum conversions
        if (underlyingType.IsEnum && IsIntegerType(sourceType))
        {
            var enumUnderlyingType = Enum.GetUnderlyingType(underlyingType);
            var converted = Convert.ChangeType(value, enumUnderlyingType);
            return Enum.ToObject(underlyingType, converted);
        }

        if (underlyingType == typeof(char) && value is string { Length: 1 } s)
            return s[0];

        var delegateInstance = LambdaDelegateConverter.TryConvert(value, underlyingType);
        if (delegateInstance != null)
            return delegateInstance;

        throw CreateImplicitConversionException(sourceType, targetType, value);
    }

    /// <summary>
    /// Validates assignment and returns the coerced value, or throws if not implicitly convertible.
    /// </summary>
    public static object? ValidateAssignment(Type targetType, object? value, string varName, bool isConstantExpression = true)
    {
        if (value == null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                throw new AlderException(DiagnosticDescriptors.NullToNonNullable, targetType.Name);
            return null;
        }

        var sourceType = value.GetType();

        if (sourceType == targetType)
            return value;

        var underlyingTarget = Nullable.GetUnderlyingType(targetType);
        if (underlyingTarget != null)
        {
            if (sourceType == underlyingTarget)
                return value;
            if (ImplicitConversions.TryGetValue(sourceType, out var nullableTargets) && nullableTargets.Contains(underlyingTarget))
                return ConvertNumeric(value, sourceType, underlyingTarget);
        }

        if (!targetType.IsValueType && targetType.IsAssignableFrom(sourceType))
            return value;

        // Allow assigning any T[] to object?[] variables (array covariance for collection expressions)
        if (targetType == typeof(object?[]) && sourceType.IsArray)
            return value;

        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(targetType))
            return ConvertNumeric(value, sourceType, targetType);

        // ECMA-334 §10.2.11: implicit constant expression conversion for int literals.
        if (isConstantExpression && sourceType == typeof(int) && value is int intValue && IsConstantIntConversionTarget(targetType))
        {
            try { return Convert.ChangeType(intValue, targetType); }
            catch (OverflowException)
            {
                throw new AlderException(
                    DiagnosticDescriptors.ConstantValueCannotConvert,
                    intValue,
                    targetType.Name);
            }
        }

        var delegateInstance = LambdaDelegateConverter.TryConvert(value, targetType);
        if (delegateInstance != null)
            return delegateInstance;

        throw CreateImplicitConversionException(sourceType, targetType, value);
    }

    private static AlderException CreateImplicitConversionException(Type sourceType, Type targetType, object? value)
    {
        var underlyingTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (sourceType == typeof(int) &&
            value is int intValue &&
            IsConstantIntConversionTarget(underlyingTarget))
        {
            try
            {
                Convert.ChangeType(intValue, underlyingTarget);
            }
            catch (OverflowException)
            {
                return new AlderException(
                    DiagnosticDescriptors.ConstantValueCannotConvert,
                    intValue,
                    underlyingTarget.Name);
            }
        }

        if (IsNumericOrCharType(sourceType) && IsNumericOrCharType(underlyingTarget))
        {
            return new AlderException(
                DiagnosticDescriptors.ExplicitConversionExists,
                sourceType.Name,
                targetType.Name);
        }

        return new AlderException(
            DiagnosticDescriptors.NoImplicitConversion,
            sourceType.Name,
            targetType.Name);
    }

    internal static T CoerceToType<T>(object? value)
    {
        if (value is T typed)
            return typed;

        var targetType = typeof(T);
        var numericTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (IsArithmetic(numericTarget) && TryCoerceNumeric(value, targetType, out var coerced) && coerced is T coercedTyped)
            return coercedTyped;

        return (T)value!;
    }

    internal static bool TryCoerceNumeric(object? arg, Type targetType, out object? result)
    {
        if (arg == null) { result = null; return true; }
        if (targetType.IsInstanceOfType(arg)) { result = arg; return true; }

        if (arg is IConvertible)
        {
            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
            try
            {
                result = Convert.ChangeType(arg, underlying);
                return true;
            }
            catch (Exception ex) when (ex is InvalidCastException or OverflowException or FormatException)
            {
                result = null;
                return false;
            }
        }

        result = null;
        return false;
    }

    internal static object? CoerceNumeric(object? arg, Type targetType)
    {
        if (TryCoerceNumeric(arg, targetType, out var result))
            return result;

        throw new AlderException(
            DiagnosticDescriptors.NoImplicitConversion,
            arg?.GetType().Name ?? "null",
            targetType.Name);
    }
}
