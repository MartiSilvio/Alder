using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal sealed class LabelEvaluator : INodeEvaluator<BoundLabelExpr>
{
    public object? Evaluate(BoundLabelExpr node, EvaluationContext ctx)
    {
        return null;
    }
}
