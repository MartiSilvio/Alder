using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.Literal)]
internal static class LiteralEvaluator
{
    public static object? Evaluate(BoundLiteralExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        return node.Value;
    }
}
