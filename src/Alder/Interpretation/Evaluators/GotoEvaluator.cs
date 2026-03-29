using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal sealed class GotoEvaluator : INodeEvaluator<BoundGotoExpr>
{
    public object? Evaluate(BoundGotoExpr node, EvaluationContext ctx)
    {
        return ControlFlowSignal.GotoSignal(node.Label);
    }
}
