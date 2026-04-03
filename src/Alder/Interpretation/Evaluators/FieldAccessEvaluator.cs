using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.FieldAccess)]
internal static class FieldAccessEvaluator
{
    public static object? Evaluate(BoundFieldAccessExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return ResolvedCallEvaluator.EvaluatePostfixChain(chain.Value, ctx, ct);

        if (node.IsStatic)
            return TypeHelpers.GuardReflectionLeak(node.Field.GetValue(null), $"static field {node.MemberName}");

        var target = ctx.Evaluate(node.Target, ct);
        if (node.NullSafe && target == null) return null;
        return ResolvedCallEvaluator.ResolveFieldAccess(node, target, ctx, ct);
    }
}
