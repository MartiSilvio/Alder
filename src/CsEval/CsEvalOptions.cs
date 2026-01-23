namespace CsEval;

/// <summary>
/// Controls how expressions are evaluated.
/// </summary>
public enum CompilationMode
{
    /// <summary>
    /// Tree-walk only. No compilation. Best for one-off expressions.
    /// </summary>
    Disabled,

    /// <summary>
    /// Compile immediately during Parse(). Best for expressions evaluated multiple times.
    /// Non-compilable expressions automatically fall back to tree-walking.
    /// </summary>
    Eager,

    /// <summary>
    /// Only compile when Compile() is called explicitly. Default mode.
    /// Gives full control over when compilation happens.
    /// </summary>
    OnDemand
}

public sealed class CsEvalOptions
{
    public static CsEvalOptions Default => new();

    public bool IgnoreCase { get; init; } = false;
    public int MaxIterations { get; init; } = 100_000;
    public SecurityOptions Security { get; init; } = new();

    /// <summary>
    /// Controls expression compilation behavior.
    /// - Disabled: Always use tree-walking (interpreter)
    /// - Eager: Compile during Parse() for maximum performance
    /// - OnDemand: Only compile when Compile() is explicitly called (default)
    /// </summary>
    public CompilationMode CompilationMode { get; init; } = CompilationMode.OnDemand;

    public sealed class SecurityOptions
    {
        public bool SafeMode { get; init; } = false;
        public bool AllowPropertyRead { get; init; } = true;
        public bool AllowAssignment { get; init; } = true;
        public bool AllowPropertySet { get; init; } = true;
        public bool AllowIndexSet { get; init; } = true;
    }

    internal StringComparer StringComparer => IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    internal StringComparison StringComparison => IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}