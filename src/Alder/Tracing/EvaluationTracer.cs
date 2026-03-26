using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Text;

namespace Alder.Tracing;

internal sealed class EvaluationTracer
{
    private readonly SourceText _sourceText;
    private readonly Stack<TraceNode> _stack = new();

    internal TraceNode? Root { get; private set; }

    internal EvaluationTracer(SourceText sourceText)
    {
        _sourceText = sourceText;
    }

    internal void Push(BoundExpr expr)
    {
        var source = ExtractSource(expr.Span);
        var description = GetDescription(expr);
        var node = new TraceNode(expr.Kind.ToString(), source, expr.Span, description);

        if (_stack.Count > 0)
            _stack.Peek()._children.Add(node);

        _stack.Push(node);
    }

    internal void Pop(object? value)
    {
        var node = _stack.Pop();
        node.Finalize(value);

        if (_stack.Count == 0)
            Root = node;
    }

    internal void PopError(Exception ex)
    {
        var node = _stack.Pop();
        node.FinalizeError(ex);

        if (_stack.Count == 0)
            Root = node;
    }

    private string ExtractSource(TextSpan span)
    {
        if (span.IsEmpty || span.Start < 0 || span.End > _sourceText.Length)
            return string.Empty;
        return _sourceText.Text.Substring(span.Start, span.Length);
    }

    private static string? GetDescription(BoundExpr expr)
    {
        return expr switch
        {
            BoundIdentifierExpr id => id.Name,
            BoundLiteralExpr lit => lit.Value?.ToString() ?? "null",
            BoundBinaryExpr bin => TokenLexemes.GetCanonical(bin.Operator),
            BoundUnaryExpr un => TokenLexemes.GetCanonical(un.Operator),
            BoundLogicalExpr log => TokenLexemes.GetCanonical(log.Operator),
            BoundMemberAccessBase mem => mem.NullSafe ? $"?.{mem.MemberName}" : $".{mem.MemberName}",
            BoundConditionalExpr => "?:",
            BoundNullCoalesceExpr => TokenLexemes.GetCanonical(TokenType.QuestionQuestion),
            BoundCastExpr cast => $"({cast.TargetType.Name})",
            BoundAsExpr asExpr => $"as {asExpr.TargetType.Name}",
            BoundLambdaExpr lambda => $"({string.Join(", ", lambda.Parameters)}) =>",
            BoundVariableDeclExpr decl => decl.Name,
            BoundAssignExpr assign => assign.Name,
            BoundIncrementDecrementExpr inc => inc.Name,
            BoundCompoundAssignExpr compound => compound.Name,
            _ => null
        };
    }

}
