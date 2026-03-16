namespace CsEval.Runtime;

internal static class RuntimeArrayFactory
{
    public static Type GetArrayType(Type elementType, int rank = 1)
    {
        if (elementType is null) throw new ArgumentNullException(nameof(elementType));
        if (rank < 1) throw new ArgumentOutOfRangeException(nameof(rank));

        if (rank == 1)
            return Create(elementType, 0).GetType();

        var lengths = new int[rank];
        return Create(elementType, lengths).GetType();
    }

    public static Array Create(Type elementType, int length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));

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
        if (lengths is null) throw new ArgumentNullException(nameof(lengths));

        return lengths.Length == 1
            ? Create(elementType, lengths[0])
            : Array.CreateInstance(elementType, lengths);
    }

    /// <summary>
    /// Creates a multidimensional array and fills it from a flat value list in row-major order.
    /// Used by the compiled emitter for multidim array initializers.
    /// </summary>
    public static Array CreateAndFill(Type elementType, int[] dimensions, object?[] flatValues)
    {
        var array = Create(elementType, dimensions);
        var rank = dimensions.Length;
        var indices = new int[rank];

        for (var i = 0; i < flatValues.Length; i++)
        {
            var value = flatValues[i];
            if (value != null)
                value = Convert.ChangeType(value, elementType);
            array.SetValue(value, indices);

            for (var d = rank - 1; d >= 0; d--)
            {
                indices[d]++;
                if (indices[d] < dimensions[d])
                    break;
                indices[d] = 0;
            }
        }

        return array;
    }
}
