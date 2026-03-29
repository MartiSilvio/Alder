using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal sealed class DynamicMemberAccessEvaluator : INodeEvaluator<BoundDynamicMemberAccessExpr>
{
    public object? Evaluate(BoundDynamicMemberAccessExpr node, EvaluationContext ctx)
    {
        var chain = PostfixChain.TryCollect(node);
        if (chain != null) return ResolvedCallEvaluator.EvaluatePostfixChain(chain.Value, ctx);

        var target = ctx.Evaluate(node.Target);
        if (node.NullSafe && target == null) return null;
        return MemberAccess.GetMember(target, node.MemberName, ctx.Config, node.NullSafe, ctx.Context);
    }
}
