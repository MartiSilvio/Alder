using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.MemberIncrement)]
internal static class MemberIncrementEvaluator
{
    public static object? Evaluate(BoundMemberIncrementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = ctx.Evaluate(node.Target, ct);
        return AssignmentRuntime.ApplyMemberIncrement(
            target, node.MemberName, node.IsIncrement, node.IsPrefix, ctx);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundMemberIncrementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = await ctx.EvaluateAsync(node.Target, ct);
        return AssignmentRuntime.ApplyMemberIncrement(
            target, node.MemberName, node.IsIncrement, node.IsPrefix, ctx);
    }
}
