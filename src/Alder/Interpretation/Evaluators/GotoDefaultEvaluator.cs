using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal sealed class GotoDefaultEvaluator : INodeEvaluator<BoundGotoDefaultExpr>
{
    public object? Evaluate(BoundGotoDefaultExpr node, EvaluationContext ctx)
    {
        return ControlFlowSignal.GotoDefaultSignal;
    }
}
