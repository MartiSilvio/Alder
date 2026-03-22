using Alder.Compilation;
using Alder.Security;

namespace Alder;

public enum LanguageMode
{
    Standard,
    Extended
}

public sealed record AlderOptions
{
    public static AlderOptions Default => new();

    public bool IsCaseSensitive { get; init; } = true;

    public ExecutionConstraints? Constraints { get; init; }

    public SandboxOptions Sandbox { get; init; } = SandboxOptions.Trusted();

    public SecurityPolicy Security { get; init; } = SecurityPolicy.Trusted;

    internal ICompiledProvider? Compiler { get; init; }

    public LanguageMode LanguageMode { get; init; } = LanguageMode.Standard;

    public IExpressionCompiler ExpressionCompiler { get; init; } = DefaultExpressionCompiler.Instance;

    internal int MaxExpressionDepth => Constraints?.MaxExpressionDepth ?? ExecutionConstraints.DefaultMaxExpressionDepth;
    internal StringComparer StringComparer => IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
    internal StringComparison StringComparison => IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
}

public sealed record SandboxOptions
{
    public bool AllowMethodCalls { get; init; }

    public bool AllowPropertyRead { get; init; }

    public bool AllowStaticPropertyRead { get; init; }

    public bool AllowStaticFieldRead { get; init; }

    public bool AllowAssignment { get; init; }

    public bool AllowPropertySet { get; init; }

    public bool AllowIndexSet { get; init; }

    public bool AllowConstruction { get; init; }

    public HashSet<Type>? AllowedTypes { get; init; }

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

    public static SandboxOptions Safe() => new()
    {
        AllowPropertyRead = true,
        AllowAssignment = true,
        AllowPropertySet = true,
        AllowIndexSet = true
    };

    public static SandboxOptions Strict() => new()
    {
        AllowPropertyRead = true
    };

    internal bool IsTypeAllowed(Type type) => AllowedTypes == null || AllowedTypes.Contains(type);
}

public sealed class ExecutionConstraints
{
    internal const int DefaultMaxExpressionDepth = 512;

    public long? MaxStatements { get; set; }

    public TimeSpan? MaxTimeout { get; set; }

    public int MaxExpressionDepth { get; set; } = DefaultMaxExpressionDepth;
}
