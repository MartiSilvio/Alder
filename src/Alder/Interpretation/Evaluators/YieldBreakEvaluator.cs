using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.YieldBreakStatement)]
internal static class YieldBreakEvaluator
{
    public static object? Evaluate(BoundYieldBreakExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        return ControlFlowSignal.YieldBreakSignal;
    }

    public static ValueTask<object?> EvaluateAsync(BoundYieldBreakExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        return new ValueTask<object?>(ControlFlowSignal.YieldBreakSignal);
    }
}
