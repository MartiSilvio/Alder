using Alder.Parsing;
using Alder.Runtime;

namespace Alder;

/// <summary>
/// Represents parsed source that can be evaluated multiple times without re-parsing.
/// </summary>
public sealed class AlderExpression
{
    internal Expr Ast { get; }

    /// <summary>
    /// Gets the original source text.
    /// </summary>
    public string Source { get; }

    internal AlderExpression(string expression, Expr ast)
    {
        Source = expression;
        Ast = ast;
    }

    /// <summary>
    /// Returns the distinct unbound identifier names present in the parsed tree.
    /// </summary>
    public IReadOnlyList<string> GetVariables()
    {
        var collector = new VariableCollector();
        collector.Collect(Ast);
        return collector.Variables;
    }
}

/// <summary>
/// Delegate shape used by the compiled execution pipeline.
/// </summary>
internal delegate object? CompiledExpressionDelegate(
    AlderContext context,
    AlderConfig config,
    ExecutionConstraintState constraintState,
    CancellationToken cancellationToken);

internal enum CompiledPipeline
{
    None = 0,
    Bound = 1
}

/// <summary>
/// Captures the compiled delegate and the outcome of the latest compilation attempt.
/// </summary>
internal record CompiledExpressionInfo(
    CompiledExpressionDelegate? Delegate,
    bool IsCompilable,
    string? FailureReason,
    Exception? FailureException = null,
    CompiledPipeline Pipeline = CompiledPipeline.None,
    int? TypeVersion = null);
