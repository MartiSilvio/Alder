using Alder.Binding.BoundNodes;
using Alder.Diagnostics;

namespace Alder.Interpretation.Evaluators;

internal static class ContinueEvaluator
{
    public static object? Evaluate(BoundContinueExpr node, EvaluationContext ctx)
    {
        if (ctx.LoopDepth == 0)
            throw new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);

        return ControlFlowSignal.Continue;
    }
}
