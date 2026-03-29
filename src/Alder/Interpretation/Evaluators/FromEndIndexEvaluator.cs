using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal sealed class FromEndIndexEvaluator : INodeEvaluator<BoundIndexFromEndExpr>
{
    public object? Evaluate(BoundIndexFromEndExpr node, EvaluationContext ctx)
    {
        return new Index(Convert.ToInt32(ctx.Evaluate(node.Operand)), fromEnd: true);
    }
}
