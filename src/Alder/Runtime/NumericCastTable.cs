namespace Alder.Runtime;

/// <summary>
/// ECMA-334 §10.3.2: explicit numeric conversions between all numeric types. Implemented with
/// direct C# cast expressions so the JIT/AOT compiler lowers each conversion to the matching
/// <c>conv.*</c> IL opcode.
/// </summary>
/// <remarks>
/// Every arm is explicitly boxed to <see cref="object"/>. This is not cosmetic: without the
/// cast, C#'s best-common-type inference for a switch expression picks the widest numeric type
/// shared by all arms and silently promotes each arm to that type before boxing. The
/// <c>decimal</c>-source row exposes this trap because <c>decimal → decimal</c> is handled by
/// the identity check upstream, leaving only <c>sbyte..double</c> arms — every one of which
/// implicitly widens to <c>double</c>, so <c>(int)3.7m</c> ends up boxed as <c>Double 3.0</c>
/// instead of <c>Int32 3</c>. Boxing each arm independently forces the switch's result type to
/// <c>object</c> and preserves the converted value's runtime type.
/// </remarks>
internal static class NumericCastTable
{
    internal static object Cast(object value, TypeCode targetCode, bool isChecked) =>
        isChecked ? CastChecked(value, targetCode) : CastUnchecked(value, targetCode);

    private static object CastChecked(object value, TypeCode targetCode) => value switch
    {
        sbyte v => targetCode switch
        {
            TypeCode.Byte => (object)checked((byte)v), TypeCode.Int16 => (object)(short)v,
            TypeCode.UInt16 => (object)checked((ushort)v), TypeCode.Int32 => (object)(int)v,
            TypeCode.UInt32 => (object)checked((uint)v), TypeCode.Int64 => (object)(long)v,
            TypeCode.UInt64 => (object)checked((ulong)v), TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        byte v => targetCode switch
        {
            TypeCode.SByte => (object)checked((sbyte)v), TypeCode.Int16 => (object)(short)v,
            TypeCode.UInt16 => (object)(ushort)v, TypeCode.Int32 => (object)(int)v,
            TypeCode.UInt32 => (object)(uint)v, TypeCode.Int64 => (object)(long)v,
            TypeCode.UInt64 => (object)(ulong)v, TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        short v => targetCode switch
        {
            TypeCode.SByte => (object)checked((sbyte)v), TypeCode.Byte => (object)checked((byte)v),
            TypeCode.UInt16 => (object)checked((ushort)v), TypeCode.Int32 => (object)(int)v,
            TypeCode.UInt32 => (object)checked((uint)v), TypeCode.Int64 => (object)(long)v,
            TypeCode.UInt64 => (object)checked((ulong)v), TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        ushort v => targetCode switch
        {
            TypeCode.SByte => (object)checked((sbyte)v), TypeCode.Byte => (object)checked((byte)v),
            TypeCode.Int16 => (object)checked((short)v), TypeCode.Int32 => (object)(int)v,
            TypeCode.UInt32 => (object)(uint)v, TypeCode.Int64 => (object)(long)v,
            TypeCode.UInt64 => (object)(ulong)v, TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        int v => targetCode switch
        {
            TypeCode.SByte => (object)checked((sbyte)v), TypeCode.Byte => (object)checked((byte)v),
            TypeCode.Int16 => (object)checked((short)v), TypeCode.UInt16 => (object)checked((ushort)v),
            TypeCode.UInt32 => (object)checked((uint)v), TypeCode.Int64 => (object)(long)v,
            TypeCode.UInt64 => (object)checked((ulong)v), TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        uint v => targetCode switch
        {
            TypeCode.SByte => (object)checked((sbyte)v), TypeCode.Byte => (object)checked((byte)v),
            TypeCode.Int16 => (object)checked((short)v), TypeCode.UInt16 => (object)checked((ushort)v),
            TypeCode.Int32 => (object)checked((int)v), TypeCode.Int64 => (object)(long)v,
            TypeCode.UInt64 => (object)(ulong)v, TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        long v => targetCode switch
        {
            TypeCode.SByte => (object)checked((sbyte)v), TypeCode.Byte => (object)checked((byte)v),
            TypeCode.Int16 => (object)checked((short)v), TypeCode.UInt16 => (object)checked((ushort)v),
            TypeCode.Int32 => (object)checked((int)v), TypeCode.UInt32 => (object)checked((uint)v),
            TypeCode.UInt64 => (object)checked((ulong)v), TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        ulong v => targetCode switch
        {
            TypeCode.SByte => (object)checked((sbyte)v), TypeCode.Byte => (object)checked((byte)v),
            TypeCode.Int16 => (object)checked((short)v), TypeCode.UInt16 => (object)checked((ushort)v),
            TypeCode.Int32 => (object)checked((int)v), TypeCode.UInt32 => (object)checked((uint)v),
            TypeCode.Int64 => (object)checked((long)v), TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        float v => targetCode switch
        {
            TypeCode.SByte => (object)checked((sbyte)v), TypeCode.Byte => (object)checked((byte)v),
            TypeCode.Int16 => (object)checked((short)v), TypeCode.UInt16 => (object)checked((ushort)v),
            TypeCode.Int32 => (object)checked((int)v), TypeCode.UInt32 => (object)checked((uint)v),
            TypeCode.Int64 => (object)checked((long)v), TypeCode.UInt64 => (object)checked((ulong)v),
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        double v => targetCode switch
        {
            TypeCode.SByte => (object)checked((sbyte)v), TypeCode.Byte => (object)checked((byte)v),
            TypeCode.Int16 => (object)checked((short)v), TypeCode.UInt16 => (object)checked((ushort)v),
            TypeCode.Int32 => (object)checked((int)v), TypeCode.UInt32 => (object)checked((uint)v),
            TypeCode.Int64 => (object)checked((long)v), TypeCode.UInt64 => (object)checked((ulong)v),
            TypeCode.Single => (object)(float)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        decimal v => targetCode switch
        {
            TypeCode.SByte => (object)checked((sbyte)v), TypeCode.Byte => (object)checked((byte)v),
            TypeCode.Int16 => (object)checked((short)v), TypeCode.UInt16 => (object)checked((ushort)v),
            TypeCode.Int32 => (object)checked((int)v), TypeCode.UInt32 => (object)checked((uint)v),
            TypeCode.Int64 => (object)checked((long)v), TypeCode.UInt64 => (object)checked((ulong)v),
            TypeCode.Single => (object)(float)v, TypeCode.Double => (object)(double)v,
            _ => throw new InvalidCastException()
        },
        _ => throw new InvalidCastException()
    };

    private static object CastUnchecked(object value, TypeCode targetCode) => value switch
    {
        sbyte v => targetCode switch
        {
            TypeCode.Byte => (object)unchecked((byte)v), TypeCode.Int16 => (object)(short)v,
            TypeCode.UInt16 => (object)unchecked((ushort)v), TypeCode.Int32 => (object)(int)v,
            TypeCode.UInt32 => (object)unchecked((uint)v), TypeCode.Int64 => (object)(long)v,
            TypeCode.UInt64 => (object)unchecked((ulong)v), TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        byte v => targetCode switch
        {
            TypeCode.SByte => (object)unchecked((sbyte)v), TypeCode.Int16 => (object)(short)v,
            TypeCode.UInt16 => (object)(ushort)v, TypeCode.Int32 => (object)(int)v,
            TypeCode.UInt32 => (object)(uint)v, TypeCode.Int64 => (object)(long)v,
            TypeCode.UInt64 => (object)(ulong)v, TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        short v => targetCode switch
        {
            TypeCode.SByte => (object)unchecked((sbyte)v), TypeCode.Byte => (object)unchecked((byte)v),
            TypeCode.UInt16 => (object)unchecked((ushort)v), TypeCode.Int32 => (object)(int)v,
            TypeCode.UInt32 => (object)unchecked((uint)v), TypeCode.Int64 => (object)(long)v,
            TypeCode.UInt64 => (object)unchecked((ulong)v), TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        ushort v => targetCode switch
        {
            TypeCode.SByte => (object)unchecked((sbyte)v), TypeCode.Byte => (object)unchecked((byte)v),
            TypeCode.Int16 => (object)unchecked((short)v), TypeCode.Int32 => (object)(int)v,
            TypeCode.UInt32 => (object)(uint)v, TypeCode.Int64 => (object)(long)v,
            TypeCode.UInt64 => (object)(ulong)v, TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        int v => targetCode switch
        {
            TypeCode.SByte => (object)unchecked((sbyte)v), TypeCode.Byte => (object)unchecked((byte)v),
            TypeCode.Int16 => (object)unchecked((short)v), TypeCode.UInt16 => (object)unchecked((ushort)v),
            TypeCode.UInt32 => (object)unchecked((uint)v), TypeCode.Int64 => (object)(long)v,
            TypeCode.UInt64 => (object)unchecked((ulong)v), TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        uint v => targetCode switch
        {
            TypeCode.SByte => (object)unchecked((sbyte)v), TypeCode.Byte => (object)unchecked((byte)v),
            TypeCode.Int16 => (object)unchecked((short)v), TypeCode.UInt16 => (object)unchecked((ushort)v),
            TypeCode.Int32 => (object)unchecked((int)v), TypeCode.Int64 => (object)(long)v,
            TypeCode.UInt64 => (object)(ulong)v, TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        long v => targetCode switch
        {
            TypeCode.SByte => (object)unchecked((sbyte)v), TypeCode.Byte => (object)unchecked((byte)v),
            TypeCode.Int16 => (object)unchecked((short)v), TypeCode.UInt16 => (object)unchecked((ushort)v),
            TypeCode.Int32 => (object)unchecked((int)v), TypeCode.UInt32 => (object)unchecked((uint)v),
            TypeCode.UInt64 => (object)unchecked((ulong)v), TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        ulong v => targetCode switch
        {
            TypeCode.SByte => (object)unchecked((sbyte)v), TypeCode.Byte => (object)unchecked((byte)v),
            TypeCode.Int16 => (object)unchecked((short)v), TypeCode.UInt16 => (object)unchecked((ushort)v),
            TypeCode.Int32 => (object)unchecked((int)v), TypeCode.UInt32 => (object)unchecked((uint)v),
            TypeCode.Int64 => (object)unchecked((long)v), TypeCode.Single => (object)(float)v,
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        float v => targetCode switch
        {
            TypeCode.SByte => (object)unchecked((sbyte)v), TypeCode.Byte => (object)unchecked((byte)v),
            TypeCode.Int16 => (object)unchecked((short)v), TypeCode.UInt16 => (object)unchecked((ushort)v),
            TypeCode.Int32 => (object)unchecked((int)v), TypeCode.UInt32 => (object)unchecked((uint)v),
            TypeCode.Int64 => (object)unchecked((long)v), TypeCode.UInt64 => (object)unchecked((ulong)v),
            TypeCode.Double => (object)(double)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        double v => targetCode switch
        {
            TypeCode.SByte => (object)unchecked((sbyte)v), TypeCode.Byte => (object)unchecked((byte)v),
            TypeCode.Int16 => (object)unchecked((short)v), TypeCode.UInt16 => (object)unchecked((ushort)v),
            TypeCode.Int32 => (object)unchecked((int)v), TypeCode.UInt32 => (object)unchecked((uint)v),
            TypeCode.Int64 => (object)unchecked((long)v), TypeCode.UInt64 => (object)unchecked((ulong)v),
            TypeCode.Single => (object)(float)v, TypeCode.Decimal => (object)(decimal)v,
            _ => throw new InvalidCastException()
        },
        // §10.3.2: decimal → integer always throws on overflow regardless of the
        // checked/unchecked context, because the lowering is a method call to
        // decimal.op_Explicit (not an IL conv opcode). The unchecked wrapper therefore has
        // no effect on decimal-source narrowings, and we omit it to keep the intent clear.
        decimal v => targetCode switch
        {
            TypeCode.SByte => (object)(sbyte)v, TypeCode.Byte => (object)(byte)v,
            TypeCode.Int16 => (object)(short)v, TypeCode.UInt16 => (object)(ushort)v,
            TypeCode.Int32 => (object)(int)v, TypeCode.UInt32 => (object)(uint)v,
            TypeCode.Int64 => (object)(long)v, TypeCode.UInt64 => (object)(ulong)v,
            TypeCode.Single => (object)(float)v, TypeCode.Double => (object)(double)v,
            _ => throw new InvalidCastException()
        },
        _ => throw new InvalidCastException()
    };
}
