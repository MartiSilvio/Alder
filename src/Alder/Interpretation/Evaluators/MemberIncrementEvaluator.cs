using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.MemberIncrement)]
internal static class MemberIncrementEvaluator
{
    public static object? Evaluate(BoundMemberIncrementExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);
        return AssignmentRuntime.ApplyMemberIncrement(
            target, node.MemberName, node.IsIncrement, node.IsPrefix, ctx);
    }
}
