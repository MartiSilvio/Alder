using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal sealed class MemberCompoundAssignEvaluator : INodeEvaluator<BoundMemberCompoundAssignExpr>
{
    public object? Evaluate(BoundMemberCompoundAssignExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);
        var rightValue = ctx.Evaluate(node.Value);
        return AssignmentRuntime.ApplyMemberCompoundAssign(
            target,
            node.MemberName,
            node.Operator,
            rightValue,
            ctx.Config,
            ctx.Context,
            ctx.IsChecked);
    }
}
