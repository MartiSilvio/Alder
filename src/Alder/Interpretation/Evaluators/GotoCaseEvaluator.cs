using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal sealed class GotoCaseEvaluator : INodeEvaluator<BoundGotoCaseExpr>
{
    public object? Evaluate(BoundGotoCaseExpr node, EvaluationContext ctx)
    {
        return ControlFlowSignal.GotoCaseSignal(ctx.Evaluate(node.Value));
    }
}
