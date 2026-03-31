namespace Alder.Runtime;

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

    internal const int DefaultMaxCollectionSize = 10_000_000;

    /// <summary>
    /// Creates an array with the default maximum length limit.
    /// Used by the compiled emitter which references this overload via reflection.
    /// </summary>
    public static Array Create(Type elementType, int length)
    {
        return Create(elementType, length, DefaultMaxCollectionSize);
    }

    /// <summary>
    /// Creates an array with a configurable maximum length limit from the security policy.
    /// Used by the interpreter which has access to the engine's SecurityPolicy.
    /// </summary>
    public static Array Create(Type elementType, int length, int maxLength)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (length > maxLength)
            throw new AlderException(Diagnostics.DiagnosticDescriptors.ArrayLengthExceeded,
                length.ToString(), maxLength.ToString());
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

    /// <summary>
    /// Creates a typed array from a list of boxed values, converting each element to the target type.
    /// Used when the binder has determined the element type statically.
    /// </summary>
    public static object CreateFromValues(Type elementType, List<object?> source)
    {
        var array = Create(elementType, source.Count);
        var convertTarget = Nullable.GetUnderlyingType(elementType) ?? elementType;
        for (var i = 0; i < source.Count; i++)
            array.SetValue(CollectionFactory.ConvertElement(source[i], elementType, convertTarget), i);
        return array;
    }

    /// <summary>
    /// Creates a typed array by inferring the element type from runtime values.
    /// Fallback for extended-mode expressions where the binder doesn't determine the type.
    /// </summary>
    public static object InferAndCreateArray(List<object?> source)
    {
        if (source.Count == 0)
            return Array.Empty<object?>();

        Type? commonType = null;
        var hasNull = false;

        foreach (var item in source)
        {
            if (item == null)
            {
                hasNull = true;
                continue;
            }

            var itemType = item.GetType();
            if (commonType == null)
                commonType = itemType;
            else if (commonType != itemType)
                return source.ToArray();
        }

        if (commonType == null)
            return source.ToArray();

        if (hasNull && commonType.IsValueType)
            commonType = RuntimeGenericFactory.CloseGenericType(typeof(Nullable<>), [commonType]);

        var array = Create(commonType, source.Count);
        for (var i = 0; i < source.Count; i++)
            array.SetValue(source[i], i);
        return array;
    }
}
