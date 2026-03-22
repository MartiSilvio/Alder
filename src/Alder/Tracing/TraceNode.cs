using Alder.Text;

namespace Alder.Tracing;

public sealed class TraceNode
{
    public string NodeKind { get; }
    public string Source { get; }
    public TextSpan Span { get; }
    public object? Value { get; internal set; }
    public Type? ValueType { get; internal set; }
    public string? Description { get; }
    public string? ErrorCode { get; internal set; }
    public string? ErrorMessage { get; internal set; }
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
