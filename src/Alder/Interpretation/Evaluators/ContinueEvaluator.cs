using Alder.Binding.BoundNodes;
using Alder.Diagnostics;

namespace Alder.Interpretation.Evaluators;

internal sealed class ContinueEvaluator : INodeEvaluator<BoundContinueExpr>
{
    public object? Evaluate(BoundContinueExpr node, EvaluationContext ctx)
    {
        if (ctx.LoopDepth == 0)
            throw new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);

        return ControlFlowSignal.Continue;
    }
}
