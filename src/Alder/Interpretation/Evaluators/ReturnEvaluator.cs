using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.ReturnStatement)]
internal static class ReturnEvaluator
{
    public static object? Evaluate(BoundReturnExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = node.Value != null ? ctx.Evaluate(node.Value, ct) : null;
        return ControlFlowSignal.Return(value);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundReturnExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = node.Value != null ? await ctx.EvaluateAsync(node.Value, ct) : null;
        return ControlFlowSignal.Return(value);
    }
}
