using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.YieldReturnStatement)]
internal static class YieldReturnEvaluator
{
    public static object? Evaluate(BoundYieldReturnExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = ctx.Evaluate(node.Value, ct);
        if (ctx.YieldCallback != null)
        {
            if (!ctx.YieldCallback(value))
                return ControlFlowSignal.YieldBreakSignal;
            return null;
        }
        return ControlFlowSignal.YieldReturnSignal(value);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundYieldReturnExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = await ctx.EvaluateAsync(node.Value, ct);
        if (ctx.YieldCallback != null)
        {
            if (!ctx.YieldCallback(value))
                return ControlFlowSignal.YieldBreakSignal;
            return null;
        }
        return ControlFlowSignal.YieldReturnSignal(value);
    }
}
