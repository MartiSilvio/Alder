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

/// <summary>
/// High-level sandbox modes for common security scenarios.
/// Use SandboxOptions.Trusted/Safe/Strict() unless you need granular control.
/// </summary>
public enum SandboxMode
{
    /// <summary>
    /// Full access. Method calls, property mutations, assignments all allowed.
    /// Use for trusted internal expressions.
    /// </summary>
    Trusted,

    /// <summary>
    /// No method calls on variable objects. Property reads, assignments, LINQ, and modules allowed.
    /// Use for user-provided expressions where you want to prevent arbitrary method invocation.
    /// </summary>
    Safe,

    /// <summary>
    /// Read-only mode. No method calls, no assignments, no property/index writes.
    /// Use for untrusted expressions that should only compute values.
    /// </summary>
    Strict
}

public sealed class CsEvalOptions
{
    public static CsEvalOptions Default => new();

    public bool IgnoreCase { get; init; } = false;
    public int MaxIterations { get; init; } = 100_000;
    public SandboxOptions Sandbox { get; init; } = new();

    /// <summary>
    /// Controls expression compilation behavior.
    /// - Disabled: Always use tree-walking (interpreter)
    /// - Eager: Compile during Parse() for maximum performance
    /// - OnDemand: Only compile when Compile() is explicitly called (default)
    /// </summary>
    public CompilationMode CompilationMode { get; init; } = CompilationMode.OnDemand;

    internal StringComparer StringComparer => IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    internal StringComparison StringComparison => IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}

/// <summary>
/// Sandbox configuration with preset modes and granular overrides.
/// Start with a preset (Trusted/Safe/Strict) and override specific settings if needed.
/// </summary>
public sealed record SandboxOptions
{
    /// <summary>
    /// The base sandbox mode. Determines default values for all other settings.
    /// </summary>
    public SandboxMode Mode { get; init; } = SandboxMode.Trusted;

    // Nullable overrides - when null, use mode defaults
    private bool? _allowMethodCalls;
    private bool? _allowPropertyRead;
    private bool? _allowAssignment;
    private bool? _allowPropertySet;
    private bool? _allowIndexSet;

    /// <summary>
    /// Allow method calls on variable objects (e.g., str.ToUpper()).
    /// Default: true for Trusted, false for Safe/Strict.
    /// </summary>
    public bool AllowMethodCalls
    {
        get => _allowMethodCalls ?? Mode == SandboxMode.Trusted;
        init => _allowMethodCalls = value;
    }

    /// <summary>
    /// Allow reading properties on variable objects (e.g., str.Length).
    /// Default: true for all modes.
    /// </summary>
    public bool AllowPropertyRead
    {
        get => _allowPropertyRead ?? true;
        init => _allowPropertyRead = value;
    }

    /// <summary>
    /// Allow variable reassignment (e.g., x = 5, x++).
    /// Default: true for Trusted/Safe, false for Strict.
    /// </summary>
    public bool AllowAssignment
    {
        get => _allowAssignment ?? Mode != SandboxMode.Strict;
        init => _allowAssignment = value;
    }

    /// <summary>
    /// Allow property assignment (e.g., obj.Name = "new").
    /// Default: true for Trusted/Safe, false for Strict.
    /// </summary>
    public bool AllowPropertySet
    {
        get => _allowPropertySet ?? Mode != SandboxMode.Strict;
        init => _allowPropertySet = value;
    }

    /// <summary>
    /// Allow index assignment (e.g., arr[0] = 5).
    /// Default: true for Trusted/Safe, false for Strict.
    /// </summary>
    public bool AllowIndexSet
    {
        get => _allowIndexSet ?? Mode != SandboxMode.Strict;
        init => _allowIndexSet = value;
    }

    /// <summary>
    /// Full access mode. All operations allowed.
    /// </summary>
    public static SandboxOptions Trusted() => new() { Mode = SandboxMode.Trusted };

    /// <summary>
    /// Safe mode. Blocks method calls on variable objects.
    /// Property reads, assignments, LINQ, and modules still allowed.
    /// </summary>
    public static SandboxOptions Safe() => new() { Mode = SandboxMode.Safe };

    /// <summary>
    /// Strict read-only mode. No method calls, no mutations.
    /// Only variable declarations, reads, and pure expressions allowed.
    /// </summary>
    public static SandboxOptions Strict() => new() { Mode = SandboxMode.Strict };

    // Internal: indicates if method calls should be blocked (used by evaluator)
    internal bool BlockMethodCalls => !AllowMethodCalls;
}