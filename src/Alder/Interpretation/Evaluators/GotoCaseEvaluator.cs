using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal static class GotoCaseEvaluator
{
    public static object? Evaluate(BoundGotoCaseExpr node, EvaluationContext ctx)
    {
        return ControlFlowSignal.GotoCaseSignal(ctx.Evaluate(node.Value));
    }
}
