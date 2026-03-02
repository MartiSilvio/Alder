namespace CsEval.Runtime.Extensions;

/// <summary>
/// Three-way comparison operator: a &lt;=&gt; b returns -1, 0, or 1.
/// Inspired by Ruby/Perl/PHP spaceship operator.
/// </summary>
public static class SpaceshipOperator
{
    /// <summary>
    /// Compares <paramref name="left"/> and <paramref name="right"/>, returning -1, 0, or 1.
    /// Null is ordered before any non-null value.
    /// </summary>
    public static int Compare(object? left, object? right)
    {
        if (left == null && right == null) return 0;
        if (left == null) return -1;
        if (right == null) return 1;

        // Use Comparer<object>.Default for broad type support
        var result = Comparer<object>.Default.Compare(left, right);
        return Math.Sign(result); // Clamp to -1, 0, 1
    }
}
