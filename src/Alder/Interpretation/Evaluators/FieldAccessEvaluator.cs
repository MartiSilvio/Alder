using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal sealed class FieldAccessEvaluator : INodeEvaluator<BoundFieldAccessExpr>
{
    public object? Evaluate(BoundFieldAccessExpr node, EvaluationContext ctx)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return ResolvedCallEvaluator.EvaluatePostfixChain(chain.Value, ctx);

        if (node.IsStatic)
            return TypeHelpers.GuardReflectionLeak(node.Field.GetValue(null), $"static field {node.MemberName}");

        var target = ctx.Evaluate(node.Target);
        if (node.NullSafe && target == null) return null;
        return ResolvedCallEvaluator.ResolveFieldAccess(node, target, ctx);
    }
}
