namespace CsEval;

public sealed class CsEvalOptions
{
    public static CsEvalOptions Default => new();

    public bool IgnoreCase { get; init; } = false;

    /// <summary>
    /// Maximum number of loop iterations allowed before throwing an exception.
    /// Set to 0 or negative to disable the limit (not recommended).
    /// Default is 100,000.
    /// </summary>
    public int MaxIterations { get; init; } = 100_000;

    internal StringComparer StringComparer => IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    internal StringComparison StringComparison => IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}