using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.GotoDefaultStatement)]
internal static class GotoDefaultEvaluator
{
    public static object? Evaluate(BoundGotoDefaultExpr node, EvaluationContext ctx)
    {
        return ControlFlowSignal.GotoDefaultSignal;
    }
}
