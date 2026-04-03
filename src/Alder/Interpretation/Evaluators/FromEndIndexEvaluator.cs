using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.FromEndIndexExpression)]
internal static class FromEndIndexEvaluator
{
    public static object? Evaluate(BoundIndexFromEndExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        return new Index(Convert.ToInt32(ctx.Evaluate(node.Operand, ct)), fromEnd: true);
    }
}
