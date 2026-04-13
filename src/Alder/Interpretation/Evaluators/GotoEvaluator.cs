using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.GotoStatement)]
internal static class GotoEvaluator
{
    public static object? Evaluate(BoundGotoExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        return ControlFlowSignal.GotoSignal(node.Label, node.Source.Span);
    }
}
