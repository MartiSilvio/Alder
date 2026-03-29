using Alder.Binding.BoundNodes;
using Alder.Diagnostics;

namespace Alder.Interpretation.Evaluators;

internal sealed class BreakEvaluator : INodeEvaluator<BoundBreakExpr>
{
    public object? Evaluate(BoundBreakExpr node, EvaluationContext ctx)
    {
        if (ctx.BreakContextDepth == 0)
            throw new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);

        return ControlFlowSignal.Break;
    }
}
