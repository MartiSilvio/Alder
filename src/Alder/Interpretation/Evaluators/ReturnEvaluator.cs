using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal sealed class ReturnEvaluator : INodeEvaluator<BoundReturnExpr>
{
    public object? Evaluate(BoundReturnExpr node, EvaluationContext ctx)
    {
        var value = node.Value != null ? ctx.Evaluate(node.Value) : null;
        return ControlFlowSignal.Return(value);
    }
}
