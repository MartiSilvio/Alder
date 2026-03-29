using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal sealed class PropertyAccessEvaluator : INodeEvaluator<BoundPropertyAccessExpr>
{
    public object? Evaluate(BoundPropertyAccessExpr node, EvaluationContext ctx)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return ResolvedCallEvaluator.EvaluatePostfixChain(chain.Value, ctx);

        if (node.IsStatic)
            return TypeHelpers.GuardReflectionLeak(node.Property.GetValue(null), $"static property {node.MemberName}");

        var target = ctx.Evaluate(node.Target);
        if (node.NullSafe && target == null) return null;
        return ResolvedCallEvaluator.ResolvePropertyAccess(node, target, ctx);
    }
}
