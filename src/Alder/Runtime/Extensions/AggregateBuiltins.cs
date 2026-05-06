using Alder.Diagnostics;
using Alder.Runtime.Collections;

namespace Alder.Runtime.Extensions;

internal static class AggregateBuiltins
{
    private static Dictionary<string, Func<object?, object?>> BuildAggregates() => new()
    {
        [ExtendedBuiltInNames.Sum] = Sum,
        [ExtendedBuiltInNames.Avg] = source => Average(source),
        [ExtendedBuiltInNames.Count] = source => Count(source),
        [ExtendedBuiltInNames.Min] = Min,
        [ExtendedBuiltInNames.Max] = Max,
    };

    private static readonly FixedDictionary<string, Func<object?, object?>> Aggregates =
        FixedDictionary<string, Func<object?, object?>>.Create(BuildAggregates(), StringComparer.Ordinal);

    private static readonly FixedDictionary<string, Func<object?, object?>> AggregatesOrdinalIgnoreCase =
        FixedDictionary<string, Func<object?, object?>>.Create(BuildAggregates(), StringComparer.OrdinalIgnoreCase);

    internal static bool TryInvoke(string name, object?[] args, bool isCaseSensitive, out object? result)
    {
        result = null;
        if (args.Length != 1)
            return false;

        var lookup = isCaseSensitive ? Aggregates : AggregatesOrdinalIgnoreCase;
        if (!lookup.TryGetValue(name, out var aggregate))
            return false;

        result = aggregate(args[0]);
        return true;
    }

    internal static object? Sum(object? source)
    {
        if (source is null)
            throw new AlderException(DiagnosticDescriptors.NameNotInContext, "aggregate source");

        return source switch
        {
            IEnumerable<int> ints => ints.Sum(),
            IEnumerable<long> longs => longs.Sum(),
            IEnumerable<float> floats => floats.Sum(),
            IEnumerable<double> doubles => doubles.Sum(),
            IEnumerable<decimal> decimals => decimals.Sum(),
            IEnumerable<int?> nullInts => nullInts.Sum(),
            IEnumerable<long?> nullLongs => nullLongs.Sum(),
            IEnumerable<float?> nullFloats => nullFloats.Sum(),
            IEnumerable<double?> nullDoubles => nullDoubles.Sum(),
            IEnumerable<decimal?> nullDecimals => nullDecimals.Sum(),
            _ => throw new AlderException(DiagnosticDescriptors.BadBinaryOps, ExtendedBuiltInNames.Sum,
                source.GetType().Name, "numeric collection")
        };
    }

    internal static object? Average(object? source)
    {
        if (source is null)
            throw new AlderException(DiagnosticDescriptors.NameNotInContext, "aggregate source");

        return source switch
        {
            IEnumerable<int> ints => ints.Average(),
            IEnumerable<long> longs => longs.Average(),
            IEnumerable<float> floats => floats.Average(),
            IEnumerable<double> doubles => doubles.Average(),
            IEnumerable<decimal> decimals => decimals.Average(),
            IEnumerable<int?> nullInts => nullInts.Average(),
            IEnumerable<long?> nullLongs => nullLongs.Average(),
            IEnumerable<float?> nullFloats => nullFloats.Average(),
            IEnumerable<double?> nullDoubles => nullDoubles.Average(),
            IEnumerable<decimal?> nullDecimals => nullDecimals.Average(),
            _ => throw new AlderException(DiagnosticDescriptors.BadBinaryOps, ExtendedBuiltInNames.Avg,
                source.GetType().Name, "numeric collection")
        };
    }

    internal static int Count(object? source)
    {
        if (source is ICollection collection)
            return collection.Count;

        var count = 0;
        foreach (var _ in Enumerate(source))
            count++;
        return count;
    }

    internal static object? Min(object? source)
    {
        if (source is null)
            throw new AlderException(DiagnosticDescriptors.NameNotInContext, "aggregate source");

        return source switch
        {
            IEnumerable<int> ints => ints.Min(),
            IEnumerable<long> longs => longs.Min(),
            IEnumerable<float> floats => floats.Min(),
            IEnumerable<double> doubles => doubles.Min(),
            IEnumerable<decimal> decimals => decimals.Min(),
            IEnumerable<int?> nullInts => nullInts.Min(),
            IEnumerable<long?> nullLongs => nullLongs.Min(),
            IEnumerable<float?> nullFloats => nullFloats.Min(),
            IEnumerable<double?> nullDoubles => nullDoubles.Min(),
            IEnumerable<decimal?> nullDecimals => nullDecimals.Min(),
            IEnumerable<string> strings => strings.Min(),
            _ => throw new AlderException(DiagnosticDescriptors.BadBinaryOps, ExtendedBuiltInNames.Min,
                source.GetType().Name, "comparable collection")
        };
    }

    internal static object? Max(object? source)
    {
        if (source is null)
            throw new AlderException(DiagnosticDescriptors.NameNotInContext, "aggregate source");

        return source switch
        {
            IEnumerable<int> ints => ints.Max(),
            IEnumerable<long> longs => longs.Max(),
            IEnumerable<double> doubles => doubles.Max(),
            IEnumerable<float> floats => floats.Max(),
            IEnumerable<decimal> decimals => decimals.Max(),
            IEnumerable<int?> nullInts => nullInts.Max(),
            IEnumerable<long?> nullLongs => nullLongs.Max(),
            IEnumerable<float?> nullFloats => nullFloats.Max(),
            IEnumerable<double?> nullDoubles => nullDoubles.Max(),
            IEnumerable<decimal?> nullDecimals => nullDecimals.Max(),
            IEnumerable<string> strings => strings.Max(),
            _ => throw new AlderException(DiagnosticDescriptors.BadBinaryOps, ExtendedBuiltInNames.Max,
                source.GetType().Name, "comparable collection")
        };
    }

    private static IEnumerable<object?> Enumerate(object? source)
    {
        if (source is null)
            throw new AlderException(DiagnosticDescriptors.NameNotInContext, "aggregate source");

        if (source is string)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, "string");

        if (source is not IEnumerable enumerable)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Of(source));

        foreach (var item in enumerable)
            yield return item;
    }
}
