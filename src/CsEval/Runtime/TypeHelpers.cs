using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using CsEval.Diagnostics;

namespace CsEval.Runtime;

/// <summary>
/// Type checking, validation, and conversion utilities.
/// </summary>
internal static class TypeHelpers
{
    public static int GetSizeOf(string typeName) => typeName switch
    {
        "bool" or "Boolean" or "System.Boolean" => 1,
        "byte" or "Byte" or "System.Byte" => 1,
        "sbyte" or "SByte" or "System.SByte" => 1,
        "char" or "Char" or "System.Char" => 2,
        "short" or "Int16" or "System.Int16" => 2,
        "ushort" or "UInt16" or "System.UInt16" => 2,
        "int" or "Int32" or "System.Int32" => 4,
        "uint" or "UInt32" or "System.UInt32" => 4,
        "float" or "Single" or "System.Single" => 4,
        "long" or "Int64" or "System.Int64" => 8,
        "ulong" or "UInt64" or "System.UInt64" => 8,
        "double" or "Double" or "System.Double" => 8,
        "decimal" or "Decimal" or "System.Decimal" => 16,
        _ => throw new CsEvalException($"Cannot take the sizeof of type '{typeName}'")
    };

    public static bool RequireBoolean(object? value)
    {
        if (value is bool b)
            return b;

        throw new CsEvalException(DiagnosticDescriptors.NoImplicitConversion, value?.GetType().Name ?? "null", "bool");
    }

    public static bool RequireBooleanForLogicalOperator(object? value, string opLexeme, string otherOperandTypeName)
    {
        if (value is bool b)
            return b;

        throw new CsEvalException(
            DiagnosticDescriptors.BadBinaryOps,
            opLexeme,
            value?.GetType().Name ?? "null",
            otherOperandTypeName);
    }

    internal static bool IsInteger([NotNullWhen(true)] object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong;

    internal static bool IsNumeric([NotNullWhen(true)] object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

    /// <summary>
    /// Checks if a value can participate in arithmetic operations.
    /// Per ECMA-334 §12.4.7.2, char undergoes unary numeric promotion to int.
    /// </summary>
    internal static bool IsArithmetic([NotNullWhen(true)] object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal or char;

    internal static bool IsArithmetic(Type type) =>
        Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal or TypeCode.Char;

    private static bool IsIntegerType(Type type) =>
        Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.UInt64;

    private static bool IsNumericOrCharType(Type type) =>
        Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal or TypeCode.Char;

    private static bool IsConstantIntConversionTarget(Type type) =>
        type == typeof(sbyte) || type == typeof(byte) ||
        type == typeof(short) || type == typeof(ushort) ||
        type == typeof(uint) || type == typeof(ulong) ||
        type == typeof(char);

    /// <summary>
    /// ECMA-334 §10.2.3: Implicit numeric conversions.
    /// "There are no predefined implicit conversions to the char type, so values of the
    /// other integral types do not automatically convert to the char type." (§10.2.3)
    /// </summary>
    private static readonly Dictionary<Type, HashSet<Type>> ImplicitConversions = new()
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
    };

    /// <summary>
    /// Returns the default value for a type (ECMA-334 §12.8.20).
    /// For value types, returns the zero/false/null equivalent.
    /// For reference types and nullable types, returns null.
    /// </summary>
    public static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

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
                throw new CsEvalException(DiagnosticDescriptors.NullToNonNullable, targetType.Name);
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
            throw new CsEvalException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, "String");
        }

        if (underlyingType == typeof(object))
            return value;

        // Reference type cast: check assignability for non-value-type targets
        if (!underlyingType.IsValueType && underlyingType != typeof(string))
        {
            if (underlyingType.IsAssignableFrom(runtimeType))
                return value;
            throw new CsEvalException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, underlyingType.Name);
        }

        // C# unboxing rule: when source static type is 'object', you can only unbox to the exact boxed type
        // (long)(object)42 fails because 42 is boxed as int, not long
        if (sourceStaticType == typeof(object) && underlyingType.IsValueType && runtimeType != underlyingType)
        {
            throw new CsEvalException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, underlyingType.Name);
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
            throw new CsEvalException(DiagnosticDescriptors.NoExplicitConversion, runtimeType.Name, targetType.Name);
        }
    }

    private static readonly ConcurrentDictionary<(Type, Type, bool), Func<object, object>> CastCache = new();

    internal static object RuntimeCast(object value, Type sourceType, Type targetType, bool isChecked = false)
    {
        var converter = CastCache.GetOrAdd((sourceType, targetType, isChecked), key =>
        {
            var param = LinqExpression.Parameter(typeof(object), "value");
            var unbox = LinqExpression.Convert(param, key.Item1);
            var cast = key.Item3
                ? LinqExpression.ConvertChecked(unbox, key.Item2)
                : LinqExpression.Convert(unbox, key.Item2);
            var box = LinqExpression.Convert(cast, typeof(object));
            return LinqExpression.Lambda<Func<object, object>>(box, param).Compile();
        });
        return converter(value);
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

    public static bool IsType(object? value, Type targetType)
    {
        if (value == null)
            return false;

        var valueType = value.GetType();
        var underlyingTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return underlyingTarget.IsAssignableFrom(valueType) || valueType == underlyingTarget;
    }

    public static object? TryAs(object? value, Type targetType)
    {
        if (value == null)
            return null;

        var valueType = value.GetType();
        var underlyingTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingTarget.IsAssignableFrom(valueType) || valueType == underlyingTarget)
            return value;

        return null;
    }

    public static bool IsNullableType(Type type)
    {
        if (!type.IsValueType)
            return true;
        return Nullable.GetUnderlyingType(type) != null;
    }

    public static object? ValidateAndCoerceType(Type targetType, object? value, string varName)
    {
        if (targetType == typeof(object))
            return value;

        var isNullable = Nullable.GetUnderlyingType(targetType) != null;
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value == null)
        {
            if (targetType.IsValueType && !isNullable)
                throw new CsEvalException(DiagnosticDescriptors.NullToNonNullable, targetType.Name);
            return null;
        }

        var sourceType = value.GetType();

        if (sourceType == underlyingType || sourceType == targetType)
            return value;

        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(underlyingType))
            return ConvertNumeric(value, sourceType, underlyingType);

        // ECMA-334 §10.2.11: Implicit constant expression conversions
        // "A constant_expression of type int can be converted to type sbyte, byte, short,
        // ushort, uint, or ulong, provided the value of the constant_expression is within
        // the range of the destination type."
        // Note: In CsEval, literal int values in typed declarations arrive here as int values.
        // OverflowException from Convert.ChangeType enforces the range check.
        if (sourceType == typeof(int) && value is int intValue && IsIntegerType(underlyingType))
        {
            try { return Convert.ChangeType(intValue, underlyingType); }
            catch (OverflowException)
            {
                throw new CsEvalException(
                    DiagnosticDescriptors.ConstantValueCannotConvert,
                    intValue,
                    underlyingType.Name);
            }
        }

        if (underlyingType == typeof(char) && value is string { Length: 1 } s)
            return s[0];

        var delegateInstance = LambdaDelegateConverter.TryConvert(value, underlyingType);
        if (delegateInstance != null)
            return delegateInstance;

        throw CreateImplicitConversionException(sourceType, targetType, value);
    }

    /// <summary>
    /// Checks if a type is a System.ValueTuple generic type.
    /// ECMA-334 §10.2.13, §10.3.6 - Tuple conversions.
    /// </summary>
    public static bool IsTupleType(Type type) =>
        type.IsGenericType && type.FullName?.StartsWith("System.ValueTuple") == true;

    /// <summary>
    /// Checks if a value can be implicitly assigned to a target type per C# rules.
    /// Handles ECMA-334 §10.2.3 implicit numeric conversions,
    /// ECMA-334 §10.6.1 implicit nullable conversions (T -> T?, S -> T? where S -> T is implicit),
    /// and ECMA-334 §10.2.13 implicit tuple conversions (element-wise implicit convertibility).
    /// Used for assignment validation.
    /// </summary>
    public static bool CanImplicitlyConvert(Type sourceType, Type targetType)
    {
        if (sourceType == targetType)
            return true;

        // ECMA-334 §10.2.13: Implicit tuple conversions
        // A tuple type can be implicitly converted to another tuple type with the same arity
        // if each element can be implicitly converted.
        if (IsTupleType(sourceType) && IsTupleType(targetType))
        {
            var sourceArgs = sourceType.GetGenericArguments();
            var targetArgs = targetType.GetGenericArguments();

            if (sourceArgs.Length != targetArgs.Length)
                return false;

            return !sourceArgs
                .Where((t, i) => !CanImplicitlyConvert(t, targetArgs[i]))
                .Any();
        }

        // ECMA-334 §10.6.1: Implicit nullable conversions
        // T -> T? (identity lift) and S -> T? (where S -> T is an implicit conversion)
        var underlyingTarget = Nullable.GetUnderlyingType(targetType);
        if (underlyingTarget != null)
        {
            // T -> T?
            if (sourceType == underlyingTarget)
                return true;

            // S -> T? where S -> T is an implicit numeric conversion
            if (ImplicitConversions.TryGetValue(sourceType, out var nullableTargets) && nullableTargets.Contains(underlyingTarget))
                return true;

            // S? -> T? where S -> T is an implicit numeric conversion
            var underlyingSource = Nullable.GetUnderlyingType(sourceType);
            if (underlyingSource != null)
            {
                if (underlyingSource == underlyingTarget)
                    return true;
                if (ImplicitConversions.TryGetValue(underlyingSource, out var liftedTargets) && liftedTargets.Contains(underlyingTarget))
                    return true;
            }
        }

        // Reference type assignability
        if (!targetType.IsValueType && targetType.IsAssignableFrom(sourceType))
            return true;

        // ECMA-334 §10.2.3: Implicit numeric conversions
        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(targetType))
            return true;

        return false;
    }

    /// <summary>
    /// Validates assignment and returns the coerced value, or throws if not implicitly convertible.
    /// Assignment requires implicit convertibility.
    /// </summary>
    public static object? ValidateAssignment(Type targetType, object? value, string varName)
    {
        if (value == null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                throw new CsEvalException(DiagnosticDescriptors.NullToNonNullable, targetType.Name);
            return null;
        }

        var sourceType = value.GetType();

        // Exact match
        if (sourceType == targetType)
            return value;

        // Nullable type conversion (e.g., int to int?)
        var underlyingTarget = Nullable.GetUnderlyingType(targetType);
        if (underlyingTarget != null)
        {
            if (sourceType == underlyingTarget)
                return value;
            if (ImplicitConversions.TryGetValue(sourceType, out var nullableTargets) && nullableTargets.Contains(underlyingTarget))
                return ConvertNumeric(value, sourceType, underlyingTarget);
        }

        // Reference type assignability (e.g., derived class to base class, array covariance)
        if (!targetType.IsValueType && targetType.IsAssignableFrom(sourceType))
            return value;

        // Allow assigning any T[] to object?[] variables (array covariance for collection expressions)
        if (targetType == typeof(object?[]) && sourceType.IsArray)
            return value;

        // Implicit numeric conversions (widening)
        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(targetType))
            return ConvertNumeric(value, sourceType, targetType);

        // ECMA-334 §10.2.11: implicit constant expression conversion for int literals.
        if (sourceType == typeof(int) && value is int intValue && IsConstantIntConversionTarget(targetType))
        {
            try { return Convert.ChangeType(intValue, targetType); }
            catch (OverflowException)
            {
                throw new CsEvalException(
                    DiagnosticDescriptors.ConstantValueCannotConvert,
                    intValue,
                    targetType.Name);
            }
        }

        // Not implicitly convertible
        throw CreateImplicitConversionException(sourceType, targetType, value);
    }

    private static CsEvalException CreateImplicitConversionException(Type sourceType, Type targetType, object? value)
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
                return new CsEvalException(
                    DiagnosticDescriptors.ConstantValueCannotConvert,
                    intValue,
                    underlyingTarget.Name);
            }
        }

        if (IsNumericOrCharType(sourceType) && IsNumericOrCharType(underlyingTarget))
        {
            return new CsEvalException(
                DiagnosticDescriptors.ExplicitConversionExists,
                sourceType.Name,
                targetType.Name);
        }

        return new CsEvalException(
            DiagnosticDescriptors.NoImplicitConversion,
            sourceType.Name,
            targetType.Name);
    }

    internal static bool IsForbiddenReflectionType(Type? type)
    {
        if (type == null) return false;

        if (typeof(Type).IsAssignableFrom(type))
            return true;

        if (typeof(MemberInfo).IsAssignableFrom(type))
            return true;

        if (typeof(Assembly).IsAssignableFrom(type))
            return true;
        if (typeof(Module).IsAssignableFrom(type))
            return true;

        if (type == typeof(RuntimeTypeHandle) ||
            type == typeof(RuntimeMethodHandle) ||
            type == typeof(RuntimeFieldHandle))
            return true;

        if (typeof(MethodBody).IsAssignableFrom(type))
            return true;

        if (type.Namespace is "System.Reflection.Emit")
            return true;

        if (type.IsPointer || type == typeof(IntPtr) || type == typeof(UIntPtr))
            return true;

        if (type.IsArray && IsForbiddenReflectionType(type.GetElementType()))
            return true;

        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                if (IsForbiddenReflectionType(arg))
                    return true;
            }
        }

        return false;
    }

    public static object? GuardReflectionLeak(object? value, string context)
    {
        if (value == null) return null;

        var type = value.GetType();
        if (IsForbiddenReflectionType(type))
            throw new CsEvalException($"Access to reflection types is not allowed: {type.Name} ({context})");

        return value;
    }

    internal static object? CoerceNumeric(object? arg, Type targetType)
    {
        if (arg == null) return null;
        if (targetType.IsInstanceOfType(arg)) return arg;

        if (arg is IConvertible)
        {
            try
            {
                var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
                return Convert.ChangeType(arg, underlying);
            }
            catch (Exception ex) when (ex is InvalidCastException or OverflowException or FormatException)
            {
                return arg;
            }
        }

        return arg;
    }
}
