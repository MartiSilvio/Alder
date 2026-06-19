using System.Diagnostics.CodeAnalysis;
using Alder.Diagnostics;

namespace Alder.Runtime;

internal static partial class TypeHelpers
{
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2067",
        Justification = "Enumerable element inference uses reflected interface discovery for compatibility; NativeAOT hosts must use registered/generated types whose interface metadata is rooted.")]
    internal static Type? GetEnumerableElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];

        foreach (var iface in RuntimeTypeIntrospection.GetInterfaces(type))
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
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2067",
        Justification = "Known scalar, enum, and nullable defaults are handled explicitly; the Activator fallback is only for value types whose constructors are part of the host-rooted AOT surface.")]
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
            _ => Activator.CreateInstance(type)!
        };
    }

    /// <summary>
    /// Bridges member access on <see cref="Nullable{T}"/> across the CLR boxing boundary.
    /// A non-null nullable boxes as its underlying value, while <c>default(T?)</c> boxes to null.
    /// The binder still resolves members against <c>Nullable&lt;T&gt;</c>, so runtime access must
    /// preserve nullable semantics even when the receiver is either null or the boxed underlying value.
    /// </summary>
    public static object? GetNullablePropertyValue(Type nullableType, object? target, string memberName)
    {
        _ = Nullable.GetUnderlyingType(nullableType)
            ?? throw new InvalidOperationException($"Type '{nullableType}' is not a nullable type.");

        return memberName switch
        {
            nameof(Nullable<int>.HasValue) => target != null,
            nameof(Nullable<int>.Value) when target != null => target,
            nameof(Nullable<int>.Value) => throw new InvalidOperationException("Nullable object must have a value."),
            _ => throw new AlderException(DiagnosticDescriptors.MemberNotFound, nullableType.Name, memberName)
        };
    }

    public static object? InvokeNullableInstanceMethod(Type nullableType, object? target, string methodName, object?[] args)
    {
        var underlyingType = Nullable.GetUnderlyingType(nullableType)
            ?? throw new InvalidOperationException($"Type '{nullableType}' is not a nullable type.");

        return (methodName, args.Length) switch
        {
            (nameof(Nullable<int>.GetValueOrDefault), 0) => target ?? GetDefaultValue(underlyingType),
            (nameof(Nullable<int>.GetValueOrDefault), 1) => target ?? args[0],
            (nameof(Nullable<int>.Equals), 1) => target?.Equals(args[0]) ?? args[0] == null,
            (nameof(Nullable<int>.GetHashCode), 0) => target?.GetHashCode() ?? 0,
            (nameof(Nullable<int>.ToString), 0) => target?.ToString() ?? string.Empty,
            _ => throw new AlderException(DiagnosticDescriptors.MemberNotFound, nullableType.Name, methodName)
        };
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

    /// <summary>
    /// Checks if a type is a System.ValueTuple generic type.
    /// ECMA-334 §10.2.13, §10.3.6 - Tuple conversions.
    /// </summary>
    public static bool IsTupleType(Type type) =>
        type.IsGenericType && type.FullName?.StartsWith("System." + nameof(ValueTuple)) == true;
}
