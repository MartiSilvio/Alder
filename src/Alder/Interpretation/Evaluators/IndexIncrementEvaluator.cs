using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.IndexIncrement)]
internal static class IndexIncrementEvaluator
{
    public static object? Evaluate(BoundIndexIncrementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = ctx.Evaluate(node.Target, ct);
        var index = ctx.Evaluate(node.Index, ct);
        return AssignmentRuntime.ApplyIndexIncrement(target, index, node.IsIncrement, node.IsPrefix, ctx.Context, ctx.IsChecked);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundIndexIncrementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = await ctx.EvaluateAsync(node.Target, ct);
        var index = await ctx.EvaluateAsync(node.Index, ct);
        return AssignmentRuntime.ApplyIndexIncrement(target, index, node.IsIncrement, node.IsPrefix, ctx.Context, ctx.IsChecked);
    }
}
