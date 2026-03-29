using Alder.Binding.BoundNodes;
using Alder.Diagnostics;

namespace Alder.Interpretation.Evaluators;

internal static class BreakEvaluator
{
    public static object? Evaluate(BoundBreakExpr node, EvaluationContext ctx)
    {
        if (ctx.BreakContextDepth == 0)
            throw new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);

        return ControlFlowSignal.Break;
    }
}
