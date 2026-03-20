namespace CsEval.Runtime;

internal static class RuntimeArrayFactory
{
    public static Type GetArrayType(Type elementType, int rank = 1)
    {
        if (elementType is null) throw new ArgumentNullException(nameof(elementType));
        if (rank < 1) throw new ArgumentOutOfRangeException(nameof(rank));

        return rank == 1
            ? elementType.MakeArrayType()
            : elementType.MakeArrayType(rank);
    }

    public static Array Create(Type elementType, int length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
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
