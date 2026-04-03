using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.MemberCompoundAssignment)]
internal static class MemberCompoundAssignEvaluator
{
    public static object? Evaluate(BoundMemberCompoundAssignExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);
        var rightValue = ctx.Evaluate(node.Value);
        return AssignmentRuntime.ApplyMemberCompoundAssign(target, node.MemberName, node.Operator, rightValue, ctx);
    }
}
