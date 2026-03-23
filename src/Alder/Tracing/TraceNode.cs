using Alder.Text;

namespace Alder.Tracing;

/// <summary>
/// A node in the evaluation trace tree, representing a single step in expression evaluation.
/// Each node captures the AST node kind, source text, evaluated value, and any child sub-evaluations.
/// </summary>
public sealed class TraceNode
{
    /// <summary>
    /// Gets the kind of AST node this trace represents (e.g., <c>"BinaryExpression"</c>, <c>"MethodCall"</c>).
    /// </summary>
    public string NodeKind { get; }

    /// <summary>
    /// Gets the source text corresponding to this evaluation step.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the source text span of this evaluation step.
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    /// Gets the value produced by evaluating this node, or <c>null</c> if evaluation failed or produced no value.
    /// </summary>
    public object? Value { get; internal set; }

    /// <summary>
    /// Gets the runtime type of <see cref="Value"/>, or <c>null</c> if the value is <c>null</c>.
    /// </summary>
    public Type? ValueType { get; internal set; }

    /// <summary>
    /// Gets an optional human-readable description of this evaluation step.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the formatted error code if this node's evaluation failed, or <c>null</c>.
    /// </summary>
    public string? ErrorCode { get; internal set; }

    /// <summary>
    /// Gets the error message if this node's evaluation failed, or <c>null</c>.
    /// </summary>
    public string? ErrorMessage { get; internal set; }

    /// <summary>
    /// Gets the child trace nodes representing sub-expressions evaluated within this node.
    /// </summary>
    public IReadOnlyList<TraceNode> Children => _children;

    internal readonly List<TraceNode> _children = [];

    internal TraceNode(string nodeKind, string source, TextSpan span, string? description)
    {
        NodeKind = nodeKind;
        Source = source;
        Span = span;
        Description = description;
    }

    internal void Finalize(object? value)
    {
        Value = value;
        ValueType = value?.GetType();
    }

    internal void FinalizeError(Exception ex)
    {
        ErrorCode = ex is AlderException alder ? alder.FormattedCode : ex.GetType().Name;
        ErrorMessage = ex.Message;
    }
}
