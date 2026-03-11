using System.Collections;
using CsEval.Diagnostics;

namespace CsEval.Runtime.Extensions;

internal static class AggregateBuiltins
{
    internal static bool TryInvoke(string name, object?[] args, bool isCaseSensitive, out object? result)
    {
        result = null;
        if (args.Length != 1)
            return false;

        var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        if (string.Equals(name, "sum", comparison))
        {
            result = Sum(args[0]);
            return true;
        }

        if (string.Equals(name, "avg", comparison))
        {
            result = Average(args[0]);
            return true;
        }

        if (string.Equals(name, "count", comparison))
        {
            result = Count(args[0]);
            return true;
        }

        if (string.Equals(name, "min", comparison))
        {
            result = Min(args[0]);
            return true;
        }

        if (string.Equals(name, "max", comparison))
        {
            result = Max(args[0]);
            return true;
        }

        return false;
    }

    internal static object? Sum(object? source)
    {
        var values = Enumerate(source);
        var hasValue = false;
        var usesDecimal = false;
        var usesDouble = false;
        var usesLong = false;

        decimal decimalTotal = 0m;
        double doubleTotal = 0d;
        long longTotal = 0L;
        int intTotal = 0;

        foreach (var value in values)
        {
            if (value == null)
                continue;

            hasValue = true;
            switch (value)
            {
                case int i:
                    if (usesDecimal) decimalTotal += i;
                    else if (usesDouble) doubleTotal += i;
                    else if (usesLong) longTotal += i;
                    else intTotal += i;
                    break;
                case long l:
                    if (usesDecimal) decimalTotal += l;
                    else if (usesDouble) doubleTotal += l;
                    else
                    {
                        if (!usesLong)
                        {
                            usesLong = true;
                            longTotal = intTotal;
                        }

                        longTotal += l;
                    }

                    break;
                case decimal m:
                    if (!usesDecimal)
                    {
                        usesDecimal = true;
                        decimalTotal = usesDouble
                            ? Convert.ToDecimal(doubleTotal)
                            : usesLong
                                ? longTotal
                                : intTotal;
                    }

                    decimalTotal += m;
                    break;
                case float f:
                    if (!usesDouble)
                    {
                        usesDouble = true;
                        doubleTotal = usesDecimal
                            ? Convert.ToDouble(decimalTotal)
                            : usesLong
                                ? longTotal
                                : intTotal;
                    }

                    doubleTotal += f;
                    break;
                case double d:
                    if (!usesDouble)
                    {
                        usesDouble = true;
                        doubleTotal = usesDecimal
                            ? Convert.ToDouble(decimalTotal)
                            : usesLong
                                ? longTotal
                                : intTotal;
                    }

                    doubleTotal += d;
                    break;
                case byte b:
                    if (usesDecimal) decimalTotal += b;
                    else if (usesDouble) doubleTotal += b;
                    else if (usesLong) longTotal += b;
                    else intTotal += b;
                    break;
                case sbyte sb:
                    if (usesDecimal) decimalTotal += sb;
                    else if (usesDouble) doubleTotal += sb;
                    else if (usesLong) longTotal += sb;
                    else intTotal += sb;
                    break;
                case short s:
                    if (usesDecimal) decimalTotal += s;
                    else if (usesDouble) doubleTotal += s;
                    else if (usesLong) longTotal += s;
                    else intTotal += s;
                    break;
                case ushort us:
                    if (usesDecimal) decimalTotal += us;
                    else if (usesDouble) doubleTotal += us;
                    else if (usesLong) longTotal += us;
                    else intTotal += us;
                    break;
                case uint ui:
                    if (usesDecimal) decimalTotal += ui;
                    else if (usesDouble) doubleTotal += ui;
                    else
                    {
                        if (!usesLong)
                        {
                            usesLong = true;
                            longTotal = intTotal;
                        }

                        longTotal += ui;
                    }

                    break;
                case ulong ul:
                    if (!usesDecimal)
                    {
                        usesDecimal = true;
                        decimalTotal = usesDouble
                            ? Convert.ToDecimal(doubleTotal)
                            : usesLong
                                ? longTotal
                                : intTotal;
                    }

                    decimalTotal += ul;
                    break;
                default:
                    throw new CsEvalException(
                        DiagnosticDescriptors.BadBinaryOps,
                        "sum",
                        value.GetType().Name,
                        "numeric");
            }
        }

        if (!hasValue)
            return 0;

        if (usesDecimal)
            return decimalTotal;
        if (usesDouble)
            return doubleTotal;
        if (usesLong)
            return longTotal;
        return intTotal;
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
            throw new InvalidOperationException("Sequence contains no elements");

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
            throw new InvalidOperationException("Sequence contains no elements");

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
