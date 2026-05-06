using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.DynamicMemberAccess)]
internal static class DynamicMemberAccessEvaluator
{
    public static object? Evaluate(BoundDynamicMemberAccessExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return ResolvedCallEvaluator.EvaluatePostfixChain(chain.Value, ctx, ct);

        var target = ctx.Evaluate(node.Target, ct);
        if (node.NullSafe && target == null) return null;
        return MemberAccess.GetMember(target, node.MemberName, node.NullSafe, ctx.Context);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundDynamicMemberAccessExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return await ResolvedCallEvaluator.EvaluatePostfixChainAsync(chain.Value, ctx, ct);

        var target = await ctx.EvaluateAsync(node.Target, ct);
        if (node.NullSafe && target == null) return null;
        return MemberAccess.GetMember(target, node.MemberName, node.NullSafe, ctx.Context);
    }
}
