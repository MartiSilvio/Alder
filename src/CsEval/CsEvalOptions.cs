using CsEval.Compilation;

namespace CsEval;

/// <summary>
/// Controls which syntax features are available during evaluation.
/// </summary>
public enum LanguageMode
{
    /// <summary>
    /// Strict ECMA-334 compliance. All non-standard syntax extensions are rejected.
    /// </summary>
    Standard,

    /// <summary>
    /// Extended mode. Enables all non-standard syntax sugar features
    /// (spread, object merge, collection expression literals, ===, !==, etc.)
    /// </summary>
    Extended
}

public sealed record CsEvalOptions
{
    public static CsEvalOptions Default => new();

    /// <summary>
    /// Whether member lookup is case-sensitive. Default: true (case-sensitive).
    /// Set to false for case-insensitive lookup (e.g., `entity.enTItyiD` matches `EntityID`).
    /// </summary>
    public bool IsCaseSensitive { get; init; } = true;

    /// <summary>
    /// Execution resource limits. Null means no constraints (default).
    /// When set, MaxStatements and MaxTimeout are enforced at statement boundaries.
    /// Constraints can be mutated between evaluations.
    /// </summary>
    public ExecutionConstraints? Constraints { get; init; }

    /// <summary>
    /// Maximum nesting depth for expression evaluation and compilation. The evaluator and IL compiler
    /// enforce this cap independently. The parser uses RuntimeHelpers.EnsureSufficientExecutionStack()
    /// instead, which lets the .NET runtime decide when stack space is exhausted.
    /// When exceeded, a catchable CsEvalException is thrown instead of risking an uncatchable
    /// StackOverflowException. Default: 512.
    /// </summary>
    public int MaxExpressionDepth { get; init; } = 512;

    public SandboxOptions Sandbox { get; init; } = SandboxOptions.Trusted();

    /// <summary>
    /// The compiled provider used for IL compilation. Null means interpretation only.
    /// Set via UseCompiler() extension from CsEval.Compiled package.
    /// </summary>
    internal ICompiledProvider? Compiler { get; init; }

    /// <summary>
    /// Controls which syntax features are available.
    /// Standard: strict ECMA-334 only.
    /// Extended: enables non-standard syntax sugar (spread, object merge, ===, !==, etc.)
    /// Default: Standard.
    /// </summary>
    public LanguageMode LanguageMode { get; init; } = LanguageMode.Standard;

    /// <summary>
    /// Strategy used to compile LINQ expression trees to delegates.
    /// Defaults to the built-in <see cref="System.Linq.Expressions"/> compiler.
    /// Supply an alternative implementation (e.g., FastExpressionCompiler) to override.
    /// </summary>
    public IExpressionCompiler ExpressionCompiler { get; init; } = DefaultExpressionCompiler.Instance;

    internal StringComparer StringComparer => IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
    internal StringComparison StringComparison => IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
}

/// <summary>
/// Sandbox configuration controlling which operations expressions can perform.
/// Default: deny-all. Use factory methods (Trusted/Safe/Strict) to grant permissions.
/// </summary>
public sealed record SandboxOptions
{
    /// <summary>
    /// Allow method calls on variable objects (e.g., str.ToUpper()).
    /// Default: false. Modules, registered functions, lambdas, and extension methods (LINQ)
    /// are always allowed regardless of this setting.
    /// </summary>
    public bool AllowMethodCalls { get; init; }

    /// <summary>
    /// Allow reading properties/fields on variable objects (e.g., str.Length).
    /// Default: false.
    /// </summary>
    public bool AllowPropertyRead { get; init; }

    /// <summary>
    /// Allow reading static properties from Type targets (e.g., int.MaxValue).
    /// Default: false.
    /// </summary>
    public bool AllowStaticPropertyRead { get; init; }

    /// <summary>
    /// Allow reading static fields from Type targets.
    /// Default: false.
    /// </summary>
    public bool AllowStaticFieldRead { get; init; }

    /// <summary>
    /// Allow variable reassignment (e.g., x = 5, x++, x += 1).
    /// Default: false. Variable declarations (var x = 5) are always allowed.
    /// </summary>
    public bool AllowAssignment { get; init; }

    /// <summary>
    /// Allow property/field assignment on objects (e.g., obj.Name = "new").
    /// Default: false.
    /// </summary>
    public bool AllowPropertySet { get; init; }

    /// <summary>
    /// Allow index assignment (e.g., arr[0] = 5, dict["key"] = value).
    /// Default: false.
    /// </summary>
    public bool AllowIndexSet { get; init; }

    /// <summary>
    /// Allow object construction via new expressions (e.g., new List&lt;int&gt;()).
    /// Default: false. When false, all new expressions are blocked.
    /// </summary>
    public bool AllowConstruction { get; init; }

    /// <summary>
    /// When set, only types in this set may be resolved, constructed, or accessed.
    /// Null means no restriction (all types in registered assemblies are available).
    /// Use with Safe() or Strict() to create a tight allowlist.
    /// </summary>
    public HashSet<Type>? AllowedTypes { get; init; }

    /// <summary>
    /// Full access mode. All operations are allowed.
    /// Use for trusted internal expressions.
    /// </summary>
    public static SandboxOptions Trusted() => new()
    {
        AllowMethodCalls = true,
        AllowPropertyRead = true,
        AllowStaticPropertyRead = true,
        AllowStaticFieldRead = true,
        AllowAssignment = true,
        AllowPropertySet = true,
        AllowIndexSet = true,
        AllowConstruction = true
    };

    /// <summary>
    /// Safe mode. Blocks method calls and object construction on variable objects.
    /// Property reads, assignments, LINQ, and modules still allowed.
    /// </summary>
    public static SandboxOptions Safe() => new()
    {
        AllowPropertyRead = true,
        AllowAssignment = true,
        AllowPropertySet = true,
        AllowIndexSet = true
    };

    /// <summary>
    /// Strict read-only mode. No method calls, no mutations, no construction.
    /// Only variable declarations, reads, property reads, and pure expressions allowed.
    /// </summary>
    public static SandboxOptions Strict() => new()
    {
        AllowPropertyRead = true
    };

    internal bool IsTypeAllowed(Type type) => AllowedTypes == null || AllowedTypes.Contains(type);
}
