using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.SliceExpression)]
internal static class SliceEvaluator
{
    public static object? Evaluate(BoundSliceExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = ctx.Evaluate(node.Target, ct);
        var start = node.Start != null ? ctx.Evaluate(node.Start, ct) : null;
        var end = node.End != null ? ctx.Evaluate(node.End, ct) : null;
        var step = node.Step != null ? ctx.Evaluate(node.Step, ct) : null;
        return MemberAccess.GetSlice(target, start, end, step);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundSliceExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = await ctx.EvaluateAsync(node.Target, ct);
        var start = node.Start != null ? await ctx.EvaluateAsync(node.Start, ct) : null;
        var end = node.End != null ? await ctx.EvaluateAsync(node.End, ct) : null;
        var step = node.Step != null ? await ctx.EvaluateAsync(node.Step, ct) : null;
        return MemberAccess.GetSlice(target, start, end, step);
    }
}
