using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal sealed class NullCoalesceEvaluator : INodeEvaluator<BoundNullCoalesceExpr>
{
    public object? Evaluate(BoundNullCoalesceExpr node, EvaluationContext ctx)
    {
        var left = ctx.Evaluate(node.Left);
        return left ?? ctx.Evaluate(node.Right);
    }
}
