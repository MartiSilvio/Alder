using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal sealed class MemberNullCoalesceAssignEvaluator : INodeEvaluator<BoundMemberNullCoalesceAssignExpr>
{
    public object? Evaluate(BoundMemberNullCoalesceAssignExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);
        target = ExecutionRuntime.EnsureMemberTargetNotNull(target, node.MemberName);
        var currentValue = MemberAccess.GetMember(target, node.MemberName, ctx.Config, nullSafe: false, ctx.Context);
        if (currentValue != null)
            return currentValue;
        var newValue = ctx.Evaluate(node.Value);
        MemberAccess.SetMember(target, node.MemberName, newValue, ctx.Config, ctx.Context);
        return newValue;
    }
}
