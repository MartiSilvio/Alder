using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.PropertyAccess)]
internal static class PropertyAccessEvaluator
{
    public static object? Evaluate(BoundPropertyAccessExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return ResolvedCallEvaluator.EvaluatePostfixChain(chain.Value, ctx, ct);

        if (node.IsStatic)
            return TypeHelpers.GuardReflectionLeak(node.Property.GetValue(null), $"static property {node.MemberName}");

        var target = ctx.Evaluate(node.Target, ct);
        if (node.NullSafe && target == null) return null;
        return ResolvedCallEvaluator.ResolvePropertyAccess(node, target, ctx, ct);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundPropertyAccessExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return await ResolvedCallEvaluator.EvaluatePostfixChainAsync(chain.Value, ctx, ct);

        if (node.IsStatic)
            return TypeHelpers.GuardReflectionLeak(node.Property.GetValue(null), $"static property {node.MemberName}");

        var target = await ctx.EvaluateAsync(node.Target, ct);
        if (node.NullSafe && target == null) return null;
        return ResolvedCallEvaluator.ResolvePropertyAccess(node, target, ctx, ct);
    }
}
