using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.BreakStatement)]
internal static class BreakEvaluator
{
    public static object? Evaluate(BoundBreakExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        if (ctx.BreakContextDepth == 0)
            throw new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);

        return ControlFlowSignal.Break;
    }
}
