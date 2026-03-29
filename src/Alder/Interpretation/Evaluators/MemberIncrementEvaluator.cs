using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal sealed class MemberIncrementEvaluator : INodeEvaluator<BoundMemberIncrementExpr>
{
    public object? Evaluate(BoundMemberIncrementExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);
        return AssignmentRuntime.ApplyMemberIncrement(
            target,
            node.MemberName,
            node.IsIncrement,
            node.IsPrefix,
            ctx.Config,
            ctx.Context,
            ctx.IsChecked);
    }
}
