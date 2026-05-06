using Alder.Diagnostics;

namespace Alder.Runtime;

internal static partial class TypeHelpers
{
    /// <summary>
    /// Mirrors Roslyn's <c>ConversionsBase.IsValidFunctionTypeConversionTarget</c>
    /// (ConversionsBase.cs:2828): a lambda or method group can bind to an inferred delegate type
    /// when the declared target is either <see cref="object"/>, <see cref="Delegate"/>,
    /// <see cref="MulticastDelegate"/>, or a non-generic <c>System.Linq.Expressions.Expression</c>.
    /// Callers that classify a lambda-to-non-delegate failure use this to choose between CS8917
    /// (delegate type could not be inferred) and CS1660 (not a delegate type).
    /// </summary>
    public static bool IsValidFunctionTypeConversionTarget(Type target) =>
        target == typeof(object)
        || target == typeof(Delegate)
        || target == typeof(MulticastDelegate)
        || target == typeof(System.Linq.Expressions.Expression);

    /// <summary>
    /// Performs an explicit cast with optional static-type guidance.
    /// When <paramref name="sourceStaticType"/> is <see cref="object"/>, the runtime enforces C# unboxing semantics
    /// and only allows unboxing to the exact boxed value type.
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

        // Exact runtime-type matches do not require any further conversion work.
        if (runtimeType == underlyingType || runtimeType == targetType)
            return value;

        // String and object are handled explicitly because their cast behavior does not fit the numeric pipeline.
        if (underlyingType == typeof(string))
        {
            if (value is char c)
                return c.ToString();
            throw new AlderException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, "String");
        }

        if (underlyingType == typeof(object))
            return value;

        // Delegate conversion is shared with assignment and invocation paths.
        var delegateInstance = LambdaDelegateConverter.TryConvert(value, underlyingType);
        if (delegateInstance != null)
            return delegateInstance;

        // For reference types, explicit cast semantics reduce to assignability after the special cases above.
        if (!underlyingType.IsValueType && underlyingType != typeof(string))
        {
            if (underlyingType.IsAssignableFrom(runtimeType))
                return value;
            throw new AlderException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, underlyingType.Name);
        }

        // C# unboxing requires the exact boxed value type. Numeric widening does not happen during unboxing.
        if (sourceStaticType == typeof(object) && underlyingType.IsValueType && runtimeType != underlyingType)
        {
            throw new AlderException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, underlyingType.Name);
        }

        // Numeric and char conversions are centralized so checked and unchecked contexts stay consistent.
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
    /// Performs explicit numeric conversion between numeric and <see cref="char"/> types.
    /// ECMA-334 §10.3.2 defines the available conversions, and checked behavior follows the active context.
    /// </summary>
    private static object NumericCast(object value, Type sourceType, Type targetType, bool isChecked)
    {
        if (sourceType == targetType)
            return value;

        var targetCode = Type.GetTypeCode(targetType);

        // ECMA-334 §8.3.6: char participates numerically through its 16-bit unsigned representation.
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
    /// Converts a numeric value to a target type, handling <see cref="char"/> through its underlying unsigned representation.
    /// </summary>
    private static object ConvertNumeric(object value, Type sourceType, Type targetType)
    {
        // Convert.ChangeType does not handle every char-to-numeric path that C# permits through the underlying integral value.
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

        // ECMA-334 §10.2.11 permits additional conversions for integral constant expressions.
        if (isConstantExpression && sourceType == typeof(int) && value is int intValue && IsIntegerType(underlyingType) && !underlyingType.IsEnum)
        {
            try { return Convert.ChangeType(intValue, underlyingType); }
            catch (OverflowException ex)
            {
                throw new AlderException(
                    DiagnosticDescriptors.ConstantValueCannotConvert,
                    ex,
                    intValue,
                    underlyingType.Name);
            }
        }

        // ECMA-334 §10.2.11 allows long-to-ulong for non-negative constants.
        if (isConstantExpression && sourceType == typeof(long) && value is long longValue && underlyingType == typeof(ulong))
        {
            if (longValue >= 0)
                return (ulong)longValue;
            throw new AlderException(DiagnosticDescriptors.ConstantValueCannotConvert, longValue, underlyingType.Name);
        }

        // Enum assignment goes through the underlying integral representation.
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

        ThrowIfLambdaConversionFailed(value, underlyingType);

        throw CreateImplicitConversionException(sourceType, targetType, value);
    }

    // §10.7.1: lambda conversions have their own diagnostic codes. If the target is not a delegate
    // type, Roslyn emits CS1660. If the target is a delegate but the arity doesn't match, CS1593.
    private static void ThrowIfLambdaConversionFailed(object? value, Type underlyingType)
    {
        if (value is not (LambdaValue or CompiledLambdaValue))
            return;

        if (!typeof(Delegate).IsAssignableFrom(underlyingType))
            throw new AlderException(DiagnosticDescriptors.LambdaToNonDelegate, underlyingType.Name);

        if (DelegateShapeInspector.TryGetInvoke(underlyingType, out var invoke))
        {
            var delegateArity = invoke.GetParameters().Length;
            var lambdaArity = value switch
            {
                LambdaValue lv => lv.Parameters.Count,
                CompiledLambdaValue cv => cv.Parameters.Count,
                _ => -1
            };
            if (lambdaArity >= 0 && lambdaArity != delegateArity)
                throw new AlderException(DiagnosticDescriptors.DelegateWrongArgumentCount,
                    FormatDelegateName(underlyingType), lambdaArity.ToString());
        }
    }

    private static string FormatDelegateName(Type delegateType)
    {
        if (!delegateType.IsGenericType)
            return delegateType.Name;

        var def = delegateType.GetGenericTypeDefinition();
        var baseName = def.Name;
        var tick = baseName.IndexOf('`');
        if (tick >= 0) baseName = baseName[..tick];

        var args = delegateType.GetGenericArguments();
        var argNames = new string[args.Length];
        for (var i = 0; i < args.Length; i++)
            argNames[i] = args[i].Name;
        return $"{baseName}<{string.Join(", ", argNames)}>";
    }

    /// <summary>
    /// Validates assignment and returns the coerced value when an implicit conversion exists.
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

        // Collection-expression lowering relies on CLR array covariance when the target is object[].
        if (targetType == typeof(object?[]) && sourceType.IsArray)
            return value;

        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(targetType))
            return ConvertNumeric(value, sourceType, targetType);

        // ECMA-334 §10.2.11 permits additional implicit conversions for integral constant expressions.
        if (isConstantExpression && sourceType == typeof(int) && value is int intValue && IsConstantIntConversionTarget(targetType))
        {
            try { return Convert.ChangeType(intValue, targetType); }
            catch (OverflowException ex)
            {
                throw new AlderException(
                    DiagnosticDescriptors.ConstantValueCannotConvert,
                    ex,
                    intValue,
                    targetType.Name);
            }
        }

        var delegateInstance = LambdaDelegateConverter.TryConvert(value, targetType);
        if (delegateInstance != null)
            return delegateInstance;

        ThrowIfLambdaConversionFailed(value, Nullable.GetUnderlyingType(targetType) ?? targetType);

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
