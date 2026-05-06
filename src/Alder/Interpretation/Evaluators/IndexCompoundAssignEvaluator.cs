using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.IndexCompoundAssignment)]
internal static class IndexCompoundAssignEvaluator
{
    public static object? Evaluate(BoundIndexCompoundAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = ctx.Evaluate(node.Target, ct);
        var index = ctx.Evaluate(node.Index, ct);
        var rightValue = ctx.Evaluate(node.Value, ct);
        return AssignmentRuntime.ApplyIndexCompoundAssign(target, index, node.Operator, rightValue, ctx.Context, ctx.IsChecked);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundIndexCompoundAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = await ctx.EvaluateAsync(node.Target, ct);
        var index = await ctx.EvaluateAsync(node.Index, ct);
        var rightValue = await ctx.EvaluateAsync(node.Value, ct);
        return AssignmentRuntime.ApplyIndexCompoundAssign(target, index, node.Operator, rightValue, ctx.Context, ctx.IsChecked);
    }
}
