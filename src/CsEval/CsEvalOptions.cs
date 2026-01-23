namespace CsEval;

public sealed class CsEvalOptions
{
    public static CsEvalOptions Default => new();

    public bool IgnoreCase { get; init; } = false;
    public int MaxIterations { get; init; } = 100_000;
    public SecurityOptions Security { get; init; } = new();

    public sealed class SecurityOptions
    {
        public bool SafeMode { get; init; } = false;
        public bool AllowPropertyRead { get; init; } = true;
        public bool AllowAssignment { get; init; } = true;
    }

    internal StringComparer StringComparer => IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    internal StringComparison StringComparison => IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}