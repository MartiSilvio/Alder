using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.CompoundAssignmentOperator)]
internal static class CompoundAssignEvaluator
{
    public static object? Evaluate(BoundCompoundAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var rightValue = ctx.Evaluate(node.Value, ct);
        return AssignmentRuntime.ApplyCompoundAssign(node.Name, node.Operator, rightValue, ctx.Context, ctx.IsChecked);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundCompoundAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var rightValue = await ctx.EvaluateAsync(node.Value, ct);
        return AssignmentRuntime.ApplyCompoundAssign(node.Name, node.Operator, rightValue, ctx.Context, ctx.IsChecked);
    }
}
