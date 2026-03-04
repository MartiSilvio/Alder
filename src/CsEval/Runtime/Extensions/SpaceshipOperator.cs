namespace CsEval.Runtime.Extensions;

/// <summary>
/// Three-way comparison operator: a &lt;=&gt; b returns -1, 0, or 1.
/// Inspired by Ruby/Perl/PHP spaceship operator.
/// </summary>
internal static class SpaceshipOperator
{
    /// <summary>
    /// Compares <paramref name="left"/> and <paramref name="right"/>, returning -1, 0, or 1.
    /// Null is ordered before any non-null value.
    /// Uses NumericDispatch for arithmetic types to handle mixed numeric comparisons (e.g., int vs double).
    /// </summary>
    public static int Compare(object? left, object? right)
    {
        if (left == null && right == null) return 0;
        if (left == null) return -1;
        if (right == null) return 1;

        // Numeric types need NumericDispatch to handle cross-type comparisons (int vs double, etc.)
        if (TypeHelpers.IsArithmetic(left) && TypeHelpers.IsArithmetic(right))
            return Math.Sign(NumericDispatch.Compare(left, right));

        // Fall back to Comparer<object>.Default for strings, IComparable, etc.
        var result = Comparer<object>.Default.Compare(left, right);
        return Math.Sign(result); // Clamp to -1, 0, 1
    }
}
