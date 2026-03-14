using CsEval.Diagnostics;

namespace CsEval.Runtime.Extensions;

/// <summary>
/// Generates integer sequences for range literals (1..10, 1..&lt;10).
/// Uses yield return for lazy evaluation -- ranges are not materialized until iterated.
/// </summary>
internal static class RangeHelpers
{
    /// <summary>
    /// Generates a sequence of integers from <paramref name="start"/> to <paramref name="end"/>.
    /// Inclusive: start..end yields [start, start+1, ..., end].
    /// Exclusive: start..&lt;end yields [start, start+1, ..., end-1].
    /// When start >= effective limit, yields empty (Ruby convention: 10..1 is empty).
    /// </summary>
    public static IEnumerable<int> GenerateRange(int start, int end, bool exclusiveEnd)
    {
        int limit = exclusiveEnd ? end : end + 1;
        for (int i = start; i < limit; i++)
            yield return i;
    }

    public static object EnsureEnumerable(object collection)
    {
        if (collection is InclusiveRange inclusive)
        {
            if (inclusive.Value.Start.IsFromEnd || inclusive.Value.End.IsFromEnd)
                throw new CsEvalException(DiagnosticDescriptors.ForeachRequiresIEnumerable, "System.Range (from-end indices cannot be iterated)");
            return GenerateRange(inclusive.Value.Start.Value, inclusive.Value.End.Value, exclusiveEnd: false);
        }
        if (collection is Range range)
        {
            if (range.Start.IsFromEnd || range.End.IsFromEnd)
                throw new CsEvalException(DiagnosticDescriptors.ForeachRequiresIEnumerable, "System.Range (from-end indices cannot be iterated)");
            return GenerateRange(range.Start.Value, range.End.Value, exclusiveEnd: true);
        }
        return collection;
    }
}
