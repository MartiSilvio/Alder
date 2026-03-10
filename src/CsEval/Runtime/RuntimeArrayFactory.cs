namespace CsEval.Runtime;

internal static class RuntimeArrayFactory
{
    public static Type GetArrayType(Type elementType, int rank = 1)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        ArgumentOutOfRangeException.ThrowIfLessThan(rank, 1);

        if (rank == 1)
            return Create(elementType, 0).GetType();

        var lengths = new int[rank];
        return Create(elementType, lengths).GetType();
    }

    public static Array Create(Type elementType, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        if (elementType == typeof(bool))
            return new bool[length];
        if (elementType == typeof(char))
            return new char[length];
        if (elementType == typeof(sbyte))
            return new sbyte[length];
        if (elementType == typeof(byte))
            return new byte[length];
        if (elementType == typeof(short))
            return new short[length];
        if (elementType == typeof(ushort))
            return new ushort[length];
        if (elementType == typeof(int))
            return new int[length];
        if (elementType == typeof(uint))
            return new uint[length];
        if (elementType == typeof(long))
            return new long[length];
        if (elementType == typeof(ulong))
            return new ulong[length];
        if (elementType == typeof(float))
            return new float[length];
        if (elementType == typeof(double))
            return new double[length];
        if (elementType == typeof(decimal))
            return new decimal[length];
        if (elementType == typeof(string))
            return new string[length];
        if (elementType == typeof(object))
            return new object[length];

        return Array.CreateInstance(elementType, length);
    }

    public static Array Create(Type elementType, int[] lengths)
    {
        ArgumentNullException.ThrowIfNull(lengths);

        return lengths.Length == 1
            ? Create(elementType, lengths[0])
            : Array.CreateInstance(elementType, lengths);
    }
}
