using Alder.Runtime.Collections;

namespace Alder.Runtime;

/// <summary>
/// ECMA-334 §10.3.2: Explicit numeric conversions between all numeric types.
/// On .NET 7+, uses INumberBase&lt;T&gt;.CreateTruncating/CreateChecked which implement
/// the correct truncate-toward-zero semantics matching C# cast IL (conv.i4 etc.).
/// On netstandard2.0, falls back to explicit cast expressions.
/// </summary>
internal static class NumericCastTable
{
    internal delegate object CastOp(object value);

#if NET7_0_OR_GREATER
    private static readonly FixedDictionary<(TypeCode, TypeCode), CastOp> TruncatingCasts = BuildTable(isChecked: false);
    private static readonly FixedDictionary<(TypeCode, TypeCode), CastOp> CheckedCasts = BuildTable(isChecked: true);

    internal static object Cast(object value, TypeCode targetCode, bool isChecked)
    {
        var sourceCode = Type.GetTypeCode(value.GetType());
        if (sourceCode == targetCode) return value;

        var table = isChecked ? CheckedCasts : TruncatingCasts;
        if (table.TryGetValue((sourceCode, targetCode), out var op))
            return op(value);

        throw new InvalidCastException(
            $"Cannot convert {value.GetType().Name} to {TypeFromCode(targetCode).Name}");
    }

    private static FixedDictionary<(TypeCode, TypeCode), CastOp> BuildTable(bool isChecked)
    {
        var d = new Dictionary<(TypeCode, TypeCode), CastOp>();
        if (isChecked)
        {
            AddChecked<sbyte>(d);  AddChecked<byte>(d);
            AddChecked<short>(d);  AddChecked<ushort>(d);
            AddChecked<int>(d);    AddChecked<uint>(d);
            AddChecked<long>(d);   AddChecked<ulong>(d);
            AddChecked<float>(d);  AddChecked<double>(d);
            AddChecked<decimal>(d);
        }
        else
        {
            AddTruncating<sbyte>(d);  AddTruncating<byte>(d);
            AddTruncating<short>(d);  AddTruncating<ushort>(d);
            AddTruncating<int>(d);    AddTruncating<uint>(d);
            AddTruncating<long>(d);   AddTruncating<ulong>(d);
            AddTruncating<float>(d);  AddTruncating<double>(d);
            AddTruncating<decimal>(d);
        }
        return FixedDictionary<(TypeCode, TypeCode), CastOp>.Create(d);
    }

    private static void AddTruncating<T>(Dictionary<(TypeCode, TypeCode), CastOp> d)
        where T : struct, System.Numerics.INumberBase<T>
    {
        var tc = Type.GetTypeCode(typeof(T));
        d[(TypeCode.SByte,   tc)] = v => T.CreateTruncating((sbyte)v);
        d[(TypeCode.Byte,    tc)] = v => T.CreateTruncating((byte)v);
        d[(TypeCode.Int16,   tc)] = v => T.CreateTruncating((short)v);
        d[(TypeCode.UInt16,  tc)] = v => T.CreateTruncating((ushort)v);
        d[(TypeCode.Int32,   tc)] = v => T.CreateTruncating((int)v);
        d[(TypeCode.UInt32,  tc)] = v => T.CreateTruncating((uint)v);
        d[(TypeCode.Int64,   tc)] = v => T.CreateTruncating((long)v);
        d[(TypeCode.UInt64,  tc)] = v => T.CreateTruncating((ulong)v);
        d[(TypeCode.Single,  tc)] = v => T.CreateTruncating((float)v);
        d[(TypeCode.Double,  tc)] = v => T.CreateTruncating((double)v);
        d[(TypeCode.Decimal, tc)] = v => T.CreateTruncating((decimal)v);
    }

    private static void AddChecked<T>(Dictionary<(TypeCode, TypeCode), CastOp> d)
        where T : struct, System.Numerics.INumberBase<T>
    {
        var tc = Type.GetTypeCode(typeof(T));
        d[(TypeCode.SByte,   tc)] = v => T.CreateChecked((sbyte)v);
        d[(TypeCode.Byte,    tc)] = v => T.CreateChecked((byte)v);
        d[(TypeCode.Int16,   tc)] = v => T.CreateChecked((short)v);
        d[(TypeCode.UInt16,  tc)] = v => T.CreateChecked((ushort)v);
        d[(TypeCode.Int32,   tc)] = v => T.CreateChecked((int)v);
        d[(TypeCode.UInt32,  tc)] = v => T.CreateChecked((uint)v);
        d[(TypeCode.Int64,   tc)] = v => T.CreateChecked((long)v);
        d[(TypeCode.UInt64,  tc)] = v => T.CreateChecked((ulong)v);
        d[(TypeCode.Single,  tc)] = v => T.CreateChecked((float)v);
        d[(TypeCode.Double,  tc)] = v => T.CreateChecked((double)v);
        d[(TypeCode.Decimal, tc)] = v => T.CreateChecked((decimal)v);
    }
#else
    // netstandard2.0 fallback: explicit cast expressions
    internal static object Cast(object value, TypeCode targetCode, bool isChecked) =>
        isChecked ? CastChecked(value, targetCode) : CastUnchecked(value, targetCode);

    private static object CastChecked(object value, TypeCode targetCode) => value switch
    {
        sbyte v => targetCode switch
        {
            TypeCode.Byte => checked((byte)v), TypeCode.Int16 => (short)v,
            TypeCode.UInt16 => checked((ushort)v), TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => checked((uint)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => checked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        byte v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Int16 => (short)v,
            TypeCode.UInt16 => (ushort)v, TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => (uint)v, TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => (ulong)v, TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        short v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.UInt16 => checked((ushort)v), TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => checked((uint)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => checked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        ushort v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => (uint)v, TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => (ulong)v, TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        int v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.UInt16 => checked((ushort)v),
            TypeCode.UInt32 => checked((uint)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => checked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        uint v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.UInt16 => checked((ushort)v),
            TypeCode.Int32 => checked((int)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => (ulong)v, TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        long v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.UInt16 => checked((ushort)v),
            TypeCode.Int32 => checked((int)v), TypeCode.UInt32 => checked((uint)v),
            TypeCode.UInt64 => checked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        ulong v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.UInt16 => checked((ushort)v),
            TypeCode.Int32 => checked((int)v), TypeCode.UInt32 => checked((uint)v),
            TypeCode.Int64 => checked((long)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        float v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.UInt16 => checked((ushort)v),
            TypeCode.Int32 => checked((int)v), TypeCode.UInt32 => checked((uint)v),
            TypeCode.Int64 => checked((long)v), TypeCode.UInt64 => checked((ulong)v),
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        double v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.UInt16 => checked((ushort)v),
            TypeCode.Int32 => checked((int)v), TypeCode.UInt32 => checked((uint)v),
            TypeCode.Int64 => checked((long)v), TypeCode.UInt64 => checked((ulong)v),
            TypeCode.Single => (float)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        decimal v => targetCode switch
        {
            TypeCode.SByte => checked((sbyte)v), TypeCode.Byte => checked((byte)v),
            TypeCode.Int16 => checked((short)v), TypeCode.UInt16 => checked((ushort)v),
            TypeCode.Int32 => checked((int)v), TypeCode.UInt32 => checked((uint)v),
            TypeCode.Int64 => checked((long)v), TypeCode.UInt64 => checked((ulong)v),
            TypeCode.Single => (float)v, TypeCode.Double => (double)v,
            _ => throw new InvalidCastException()
        },
        _ => throw new InvalidCastException()
    };

    private static object CastUnchecked(object value, TypeCode targetCode) => value switch
    {
        sbyte v => targetCode switch
        {
            TypeCode.Byte => unchecked((byte)v), TypeCode.Int16 => (short)v,
            TypeCode.UInt16 => unchecked((ushort)v), TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => unchecked((uint)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => unchecked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        byte v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Int16 => (short)v,
            TypeCode.UInt16 => (ushort)v, TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => (uint)v, TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => (ulong)v, TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        short v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Byte => unchecked((byte)v),
            TypeCode.UInt16 => unchecked((ushort)v), TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => unchecked((uint)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => unchecked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        ushort v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Byte => unchecked((byte)v),
            TypeCode.Int16 => unchecked((short)v), TypeCode.Int32 => (int)v,
            TypeCode.UInt32 => (uint)v, TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => (ulong)v, TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        int v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Byte => unchecked((byte)v),
            TypeCode.Int16 => unchecked((short)v), TypeCode.UInt16 => unchecked((ushort)v),
            TypeCode.UInt32 => unchecked((uint)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => unchecked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        uint v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Byte => unchecked((byte)v),
            TypeCode.Int16 => unchecked((short)v), TypeCode.UInt16 => unchecked((ushort)v),
            TypeCode.Int32 => unchecked((int)v), TypeCode.Int64 => (long)v,
            TypeCode.UInt64 => (ulong)v, TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        long v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Byte => unchecked((byte)v),
            TypeCode.Int16 => unchecked((short)v), TypeCode.UInt16 => unchecked((ushort)v),
            TypeCode.Int32 => unchecked((int)v), TypeCode.UInt32 => unchecked((uint)v),
            TypeCode.UInt64 => unchecked((ulong)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        ulong v => targetCode switch
        {
            TypeCode.SByte => unchecked((sbyte)v), TypeCode.Byte => unchecked((byte)v),
            TypeCode.Int16 => unchecked((short)v), TypeCode.UInt16 => unchecked((ushort)v),
            TypeCode.Int32 => unchecked((int)v), TypeCode.UInt32 => unchecked((uint)v),
            TypeCode.Int64 => unchecked((long)v), TypeCode.Single => (float)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        float v => targetCode switch
        {
            TypeCode.SByte => (sbyte)v, TypeCode.Byte => (byte)v,
            TypeCode.Int16 => (short)v, TypeCode.UInt16 => (ushort)v,
            TypeCode.Int32 => (int)v, TypeCode.UInt32 => (uint)v,
            TypeCode.Int64 => (long)v, TypeCode.UInt64 => (ulong)v,
            TypeCode.Double => (double)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        double v => targetCode switch
        {
            TypeCode.SByte => (sbyte)v, TypeCode.Byte => (byte)v,
            TypeCode.Int16 => (short)v, TypeCode.UInt16 => (ushort)v,
            TypeCode.Int32 => (int)v, TypeCode.UInt32 => (uint)v,
            TypeCode.Int64 => (long)v, TypeCode.UInt64 => (ulong)v,
            TypeCode.Single => (float)v, TypeCode.Decimal => (decimal)v,
            _ => throw new InvalidCastException()
        },
        decimal v => targetCode switch
        {
            TypeCode.SByte => (sbyte)v, TypeCode.Byte => (byte)v,
            TypeCode.Int16 => (short)v, TypeCode.UInt16 => (ushort)v,
            TypeCode.Int32 => (int)v, TypeCode.UInt32 => (uint)v,
            TypeCode.Int64 => (long)v, TypeCode.UInt64 => (ulong)v,
            TypeCode.Single => (float)v, TypeCode.Double => (double)v,
            _ => throw new InvalidCastException()
        },
        _ => throw new InvalidCastException()
    };
#endif

    private static Type TypeFromCode(TypeCode code) => code switch
    {
        TypeCode.SByte => typeof(sbyte), TypeCode.Byte => typeof(byte),
        TypeCode.Int16 => typeof(short), TypeCode.UInt16 => typeof(ushort),
        TypeCode.Int32 => typeof(int), TypeCode.UInt32 => typeof(uint),
        TypeCode.Int64 => typeof(long), TypeCode.UInt64 => typeof(ulong),
        TypeCode.Single => typeof(float), TypeCode.Double => typeof(double),
        TypeCode.Decimal => typeof(decimal),
        _ => throw new ArgumentOutOfRangeException(nameof(code))
    };
}
