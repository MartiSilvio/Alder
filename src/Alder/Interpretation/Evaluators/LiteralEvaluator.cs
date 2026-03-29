using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal sealed class LiteralEvaluator : INodeEvaluator<BoundLiteralExpr>
{
    public object? Evaluate(BoundLiteralExpr node, EvaluationContext ctx)
    {
        return node.Value;
    }
}
