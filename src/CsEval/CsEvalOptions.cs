namespace CsEval;

public enum CompilationMode
{
    /// <summary>
    /// Always use tree-walking interpretation. Best for debugging or AST-only features.
    /// </summary>
    Interpreted,

    /// <summary>
    /// Compile to IL, fall back to tree-walking if compilation fails. Best for production.
    /// </summary>
    Compiled,

    /// <summary>
    /// Require IL compilation - throw if compilation fails. Best for testing IL coverage.
    /// </summary>
    StrictCompiled
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

public sealed record CsEvalOptions
{
    public static CsEvalOptions Default => new();

    /// <summary>
    /// Whether member lookup is case-sensitive. Default: true (case-sensitive).
    /// Set to false for case-insensitive lookup (e.g., `entity.enTItyiD` matches `EntityID`).
    /// </summary>
    public bool IsCaseSensitive { get; init; } = true;

    public int MaxIterations { get; init; } = 100_000;

    /// <summary>
    /// Maximum nesting depth for expression evaluation and compilation. The evaluator and IL compiler
    /// enforce this cap independently. The parser uses RuntimeHelpers.EnsureSufficientExecutionStack()
    /// instead, which lets the .NET runtime decide when stack space is exhausted.
    /// When exceeded, a catchable CsEvalException is thrown instead of risking an uncatchable
    /// StackOverflowException. Default: 512.
    /// </summary>
    public int MaxExpressionDepth { get; init; } = 512;

    public SandboxOptions Sandbox { get; init; } = new();

    /// <summary>
    /// Controls when expressions are compiled to IL.
    /// Default: Compiled (compile automatically on first evaluation, fall back to tree-walking if compilation fails).
    /// </summary>
    /// <remarks>
    /// <see cref="CompilationMode.Interpreted"/>: Always use tree-walking. Good for debugging or AST-only features.
    /// <see cref="CompilationMode.Compiled"/>: Automatically compile on first evaluation, fall back to tree-walking if compilation fails. Good for production.
    /// <see cref="CompilationMode.StrictCompiled"/>: Require IL compilation - throws if compilation fails. Good for testing IL coverage.
    /// </remarks>
    public CompilationMode CompilationMode { get; init; } = CompilationMode.Compiled;

    internal StringComparer StringComparer => IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
    internal StringComparison StringComparison => IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
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
    /// Full access mode. All operations are allowed.
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