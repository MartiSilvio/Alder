using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.IndexAssignment)]
internal static class IndexAssignEvaluator
{
    public static object? Evaluate(BoundIndexAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = ctx.Evaluate(node.Target, ct);
        var index = ctx.Evaluate(node.Index, ct);
        var value = ctx.Evaluate(node.Value, ct);
        return AssignmentRuntime.ApplyIndexAssign(target, index, value, ctx);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundIndexAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = await ctx.EvaluateAsync(node.Target, ct);
        var index = await ctx.EvaluateAsync(node.Index, ct);
        var value = await ctx.EvaluateAsync(node.Value, ct);
        return AssignmentRuntime.ApplyIndexAssign(target, index, value, ctx);
    }
}
