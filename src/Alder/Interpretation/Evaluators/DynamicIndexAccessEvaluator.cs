using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.DynamicIndexAccess)]
internal static class DynamicIndexAccessEvaluator
{
    public static object? Evaluate(BoundDynamicIndexAccessExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = ctx.Evaluate(node.Target, ct);
        if (node.NullSafe && target == null)
            return null;

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var index = ctx.Evaluate(node.Index, ct);
        return MemberAccess.GetIndex(target, index, ctx.Context);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundDynamicIndexAccessExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = await ctx.EvaluateAsync(node.Target, ct);
        if (node.NullSafe && target == null)
            return null;

        if (target == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        var index = await ctx.EvaluateAsync(node.Index, ct);
        return MemberAccess.GetIndex(target, index, ctx.Context);
    }
}
