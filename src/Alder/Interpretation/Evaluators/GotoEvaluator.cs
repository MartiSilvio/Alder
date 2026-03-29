using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal static class GotoEvaluator
{
    public static object? Evaluate(BoundGotoExpr node, EvaluationContext ctx)
    {
        return ControlFlowSignal.GotoSignal(node.Label);
    }
}
