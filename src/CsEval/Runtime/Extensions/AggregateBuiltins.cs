using CsEval.Runtime.Collections;
using CsEval.Diagnostics;

namespace CsEval.Runtime.Extensions;

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
            throw new CsEvalException(DiagnosticDescriptors.NameNotInContext, "aggregate source");

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
            _ => throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, ExtendedBuiltInNames.Sum,
                source.GetType().Name, "numeric collection")
        };
    }

    internal static double Average(object? source)
    {
        var values = Enumerate(source);
        var count = 0;
        double total = 0d;

        foreach (var value in values)
        {
            if (value == null)
                continue;

            total += Convert.ToDouble(value);
            count++;
        }

        if (count == 0)
            throw new CsEvalException(Diagnostics.DiagnosticDescriptors.SequenceContainsNoElements);

        return total / count;
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

    internal static object? Min(object? source) => Extreme(source, pickMax: false);

    internal static object? Max(object? source) => Extreme(source, pickMax: true);

    private static object? Extreme(object? source, bool pickMax)
    {
        var hasValue = false;
        object? best = null;

        foreach (var value in Enumerate(source))
        {
            if (value == null)
                continue;

            if (!hasValue)
            {
                hasValue = true;
                best = value;
                continue;
            }

            var comparison = Compare(value, best!);
            if ((pickMax && comparison > 0) || (!pickMax && comparison < 0))
                best = value;
        }

        if (!hasValue)
            throw new CsEvalException(Diagnostics.DiagnosticDescriptors.SequenceContainsNoElements);

        return best;
    }

    private static int Compare(object left, object right)
    {
        if (TypeHelpers.IsArithmetic(left) && TypeHelpers.IsArithmetic(right))
            return Convert.ToDecimal(left).CompareTo(Convert.ToDecimal(right));

        if (left is IComparable comparable && left.GetType() == right.GetType())
            return comparable.CompareTo(right);

        throw new CsEvalException(
            DiagnosticDescriptors.BadBinaryOps,
            "compare",
            left.GetType().Name,
            right.GetType().Name);
    }

    private static IEnumerable<object?> Enumerate(object? source)
    {
        if (source is null)
            throw new CsEvalException(DiagnosticDescriptors.NameNotInContext, "aggregate source");

        if (source is string)
            throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, "string");

        if (source is not IEnumerable enumerable)
            throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Of(source));

        foreach (var item in enumerable)
            yield return item;
    }
}
