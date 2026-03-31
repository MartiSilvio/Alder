using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Alder.Diagnostics;
using Alder.Runtime.Collections;

namespace Alder.Runtime;

/// <summary>
/// Type checking, validation, and conversion utilities.
/// </summary>
internal static class TypeHelpers
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

    private static readonly ConcurrentDictionary<Type, bool> ForbiddenTypeCache = new();

    internal static Type? GetEnumerableElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];

        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        }

        return null;
    }

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
        _ => throw new AlderException(DiagnosticDescriptors.SizeofUnsupportedType, typeName)
    };

    public static bool RequireBoolean(object? value)
    {
        if (value is bool b)
            return b;

        throw new AlderException(DiagnosticDescriptors.NoImplicitConversion, TypeNameFormatter.Of(value), "bool");
    }

    public static bool RequireBooleanForLogicalOperator(object? value, string opLexeme, string otherOperandTypeName)
    {
        if (value is bool b)
            return b;

        throw new AlderException(
            DiagnosticDescriptors.BadBinaryOps,
            opLexeme,
            TypeNameFormatter.Of(value),
            otherOperandTypeName);
    }

    // §12.13.5 + §12.14.2: Three-value logic for bool? && and bool? ||
    public static object? NullableBoolAnd(object? left, object? right)
    {
        var l = left as bool?;
        var r = right as bool?;
        if (l == false || r == false) return BoxedConstants.False;
        if (l == true && r == true) return BoxedConstants.True;
        return null;
    }

    public static object? NullableBoolOr(object? left, object? right)
    {
        var l = left as bool?;
        var r = right as bool?;
        if (l == true || r == true) return BoxedConstants.True;
        if (l == false && r == false) return BoxedConstants.False;
        return null;
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

    internal static bool IsValueTupleType(Type type) =>
        type is { IsValueType: true, IsGenericType: true } &&
        type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;

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
    /// Returns the default value for a type (ECMA-334 §12.8.20).
    /// For value types, returns the zero/false/null equivalent.
    /// For reference types and nullable types, returns null.
    /// </summary>
    public static object? GetDefaultValue(Type type)
    {
        if (!type.IsValueType)
            return null;

        if (Nullable.GetUnderlyingType(type) != null)
            return null;

        if (type.IsEnum)
            return Enum.ToObject(type, 0);

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => false,
            TypeCode.Char => '\0',
            TypeCode.SByte => (sbyte)0,
            TypeCode.Byte => (byte)0,
            TypeCode.Int16 => (short)0,
            TypeCode.UInt16 => (ushort)0,
            TypeCode.Int32 => 0,
            TypeCode.UInt32 => 0u,
            TypeCode.Int64 => 0L,
            TypeCode.UInt64 => 0UL,
            TypeCode.Single => 0f,
            TypeCode.Double => 0d,
            TypeCode.Decimal => 0m,
            TypeCode.DateTime => default(DateTime),
#if NET5_0_OR_GREATER
            _ => System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type)
#else
            _ => System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type)
#endif
        };
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

        if (isChecked)
            return NumericCastChecked(value, targetCode);

        return NumericCastUnchecked(value, targetCode);
    }

    private static object NumericCastChecked(object value, TypeCode targetCode) => value switch
    {
        sbyte v => targetCode switch
        {
            TypeCode.Byte => checked((byte)v), TypeCode.Int16 => checked((short)v),
            TypeCode.UInt16 => checked((ushort)v), TypeCode.Int32 => checked((int)v),
            TypeCode.UInt32 => checked((uint)v), TypeCode.Int64 => checked((long)v),
            TypeCode.UInt64 => checked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        byte v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Int16 => (short)v,
            TypeCode.UInt16 => (ushort)v, TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => (uint)v, TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => (ulong)v, TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        short v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.UInt16 => checked((ushort)v), TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => checked((uint)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => checked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        ushort v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => (uint)v, TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => (ulong)v, TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        int v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.UInt16 => checked((ushort)v),
            TypeCode.UInt32 => checked((uint)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => checked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        uint v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.UInt16 => checked((ushort)v),
            TypeCode.Int32 => checked((int)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => (ulong)v, TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        long v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.UInt16 => checked((ushort)v),
            TypeCode.Int32 => checked((int)v), TypeCode.UInt32 => checked((uint)v),
            TypeCode.UInt64 => checked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        ulong v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.UInt16 => checked((ushort)v),
            TypeCode.Int32 => checked((int)v), TypeCode.UInt32 => checked((uint)v),
            TypeCode.Int64 => checked((long)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        float v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.UInt16 => checked((ushort)v),
            TypeCode.Int32 => checked((int)v), TypeCode.UInt32 => checked((uint)v),
            TypeCode.Int64 => checked((long)v), TypeCode.UInt64 => checked((ulong)v),
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        double v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.UInt16 => checked((ushort)v),
            TypeCode.Int32 => checked((int)v), TypeCode.UInt32 => checked((uint)v),
            TypeCode.Int64 => checked((long)v), TypeCode.UInt64 => checked((ulong)v),
            TypeCode.Single => (float)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        decimal v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.UInt16 => checked((ushort)v),
            TypeCode.Int32 => checked((int)v), TypeCode.UInt32 => checked((uint)v),
            TypeCode.Int64 => checked((long)v), TypeCode.UInt64 => checked((ulong)v),
            TypeCode.Single => (float)v, TypeCode.Double => (double)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        _ => Convert.ChangeType(value, targetCode)
    };

    private static object NumericCastUnchecked(object value, TypeCode targetCode) => value switch
    {
        sbyte v => targetCode switch
        {
            TypeCode.Byte => unchecked((byte)v), TypeCode.Int16 => (short)v,
            TypeCode.UInt16 => unchecked((ushort)v), TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => unchecked((uint)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => unchecked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        byte v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Int16 => (short)v,
            TypeCode.UInt16 => (ushort)v, TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => (uint)v, TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => (ulong)v, TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        short v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Byte => unchecked((byte)v),
            TypeCode.UInt16 => unchecked((ushort)v), TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => unchecked((uint)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => unchecked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        ushort v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Byte => unchecked((byte)v),
            TypeCode.Int16 => unchecked((short)v), TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => (uint)v, TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => (ulong)v, TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        int v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Byte => unchecked((byte)v),
            TypeCode.Int16 => unchecked((short)v), TypeCode.UInt16 => unchecked((ushort)v),
            TypeCode.UInt32 => unchecked((uint)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => unchecked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        uint v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Byte => unchecked((byte)v),
            TypeCode.Int16 => unchecked((short)v), TypeCode.UInt16 => unchecked((ushort)v),
            TypeCode.Int32 => unchecked((int)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => (ulong)v, TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        long v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Byte => unchecked((byte)v),
            TypeCode.Int16 => unchecked((short)v), TypeCode.UInt16 => unchecked((ushort)v),
            TypeCode.Int32 => unchecked((int)v), TypeCode.UInt32 => unchecked((uint)v),
            TypeCode.UInt64 => unchecked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        ulong v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Byte => unchecked((byte)v),
            TypeCode.Int16 => unchecked((short)v), TypeCode.UInt16 => unchecked((ushort)v),
            TypeCode.Int32 => unchecked((int)v), TypeCode.UInt32 => unchecked((uint)v),
            TypeCode.Int64 => unchecked((long)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        float v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Byte => unchecked((byte)v),
            TypeCode.Int16 => unchecked((short)v), TypeCode.UInt16 => unchecked((ushort)v),
            TypeCode.Int32 => unchecked((int)v), TypeCode.UInt32 => unchecked((uint)v),
            TypeCode.Int64 => unchecked((long)v), TypeCode.UInt64 => unchecked((ulong)v),
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        double v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Byte => unchecked((byte)v),
            TypeCode.Int16 => unchecked((short)v), TypeCode.UInt16 => unchecked((ushort)v),
            TypeCode.Int32 => unchecked((int)v), TypeCode.UInt32 => unchecked((uint)v),
            TypeCode.Int64 => unchecked((long)v), TypeCode.UInt64 => unchecked((ulong)v),
            TypeCode.Single => (float)v, TypeCode.Decimal => (decimal)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        decimal v => targetCode switch
        {
            TypeCode.SByte => (sbyte)v, TypeCode.Byte => (byte)v,
            TypeCode.Int16 => (short)v, TypeCode.UInt16 => (ushort)v,
            TypeCode.Int32 => (int)v, TypeCode.UInt32 => (uint)v,
            TypeCode.Int64 => (long)v, TypeCode.UInt64 => (ulong)v,
            TypeCode.Single => (float)v, TypeCode.Double => (double)v,
            _ => Convert.ChangeType(v, targetCode)
        },
        _ => Convert.ChangeType(value, targetCode)
    };

    /// <summary>
    /// ECMA-334 §10.5.3–§10.5.5: Find the most specific user-defined conversion operator.
    /// Searches source/target type hierarchies, filters by standard conversion applicability,
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
                // from sourceType → paramType OR paramType → sourceType (encompassing or encompassed)
                var sourceApplicable = IsStandardImplicitConversion(sourceType, paramType)
                    || IsStandardImplicitConversion(paramType, sourceType);

                // §10.5.5: standard conversion from returnType → targetType or targetType → returnType
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
        // Prefer exact source match, then most-encompassed (most specific) type
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

        // §10.5.4: Most-encompassed source type (most specific — the one
        // that all other candidate source types can convert TO)
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
        // "A constant_expression of type int can be converted to type sbyte, byte, short,
        // ushort, uint, or ulong, provided the value of the constant_expression is within
        // the range of the destination type."
        // Note: In Alder, literal int values in typed declarations arrive here as int values.
        // OverflowException from Convert.ChangeType enforces the range check.
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

        // §10.2.4: Implicit enumeration conversion — constant 0 → any enum
        // §10.3.3: Explicit enumeration conversion — any integer ↔ enum
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
    /// Checks if a type is a System.ValueTuple generic type.
    /// ECMA-334 §10.2.13, §10.3.6 - Tuple conversions.
    /// </summary>
    public static bool IsTupleType(Type type) =>
        type.IsGenericType && type.FullName?.StartsWith("System." + nameof(ValueTuple)) == true;

    /// <summary>
    /// Checks if a value can be implicitly assigned to a target type per C# rules.
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

    /// Handles ECMA-334 §10.2.3 implicit numeric conversions,
    /// ECMA-334 §10.6.1 implicit nullable conversions (T -> T?, S -> T? where S -> T is implicit),
    /// and ECMA-334 §10.2.13 implicit tuple conversions (element-wise implicit convertibility).
    /// Used for assignment validation.
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
    /// ECMA-334 §10.4.2: Standard implicit conversions — the pre-defined conversions that can
    /// occur as part of a user-defined conversion. Does NOT include user-defined conversions,
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

    /// <summary>
    /// Checks whether a source type can be assigned or implicitly converted to a target type.
    /// This is the canonical compatibility check used by compile-time emitters when selecting
    /// method overloads and constructors.
    /// </summary>
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

    /// <summary>
    /// Validates assignment and returns the coerced value, or throws if not implicitly convertible.
    /// Assignment requires implicit convertibility.
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

        // Not implicitly convertible
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

    internal static bool IsForbiddenReflectionType(Type? type)
    {
        if (type == null) return false;

        return ForbiddenTypeCache.GetOrAdd(type, static t => IsForbiddenReflectionTypeCore(t));
    }

    private static bool IsForbiddenReflectionTypeCore(Type type)
    {
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? GuardReflectionLeak(object? value, string context)
    {
        if (value == null) return null;
        if (IsForbiddenReflectionType(value.GetType()))
            ThrowReflectionLeak(value.GetType(), context);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? GuardReflectionLeak(object? value, string memberKind, string memberName)
    {
        if (value == null) return null;
        if (IsForbiddenReflectionType(value.GetType()))
            ThrowReflectionLeak(value.GetType(), memberKind, memberName);
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowReflectionLeak(Type type, string context) =>
        throw new AlderException(DiagnosticDescriptors.ReflectionTypeAccessBlocked, type.Name, context);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowReflectionLeak(Type type, string memberKind, string memberName) =>
        throw new AlderException(DiagnosticDescriptors.ReflectionTypeAccessBlocked, type.Name, $"{memberKind} {memberName}");

    public static T GuardReflectionLeakTyped<T>(T value, string context)
    {
        if (!typeof(T).IsValueType && value is not null)
        {
            var type = value.GetType();
            if (IsForbiddenReflectionType(type))
                throw new AlderException(DiagnosticDescriptors.ReflectionTypeAccessBlocked, type.Name, context);
        }

        return value;
    }

    internal static bool RequiresReflectionLeakGuard(Type type)
    {
        if (type.IsValueType)
            return false;

        if (type == typeof(string))
            return false;

        if (type == typeof(object))
            return true;

        if (IsForbiddenReflectionType(type))
            return true;

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            return elementType == null || RequiresReflectionLeakGuard(elementType);
        }

        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                if (RequiresReflectionLeakGuard(arg))
                    return true;
            }
        }

        // For non-sealed reference types, runtime values can still be forbidden subtypes.
        return !type.IsSealed;
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
