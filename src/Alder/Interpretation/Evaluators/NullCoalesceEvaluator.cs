using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.NullCoalescingOperator)]
internal static class NullCoalesceEvaluator
{
    public static object? Evaluate(BoundNullCoalesceExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var left = ctx.Evaluate(node.Left, ct);
        return left ?? ctx.Evaluate(node.Right, ct);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundNullCoalesceExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var left = await ctx.EvaluateAsync(node.Left, ct);
        return left ?? await ctx.EvaluateAsync(node.Right, ct);
    }
}
