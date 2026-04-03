using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.MemberCompoundAssignment)]
internal static class MemberCompoundAssignEvaluator
{
    public static object? Evaluate(BoundMemberCompoundAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = ctx.Evaluate(node.Target, ct);
        var rightValue = ctx.Evaluate(node.Value, ct);
        return AssignmentRuntime.ApplyMemberCompoundAssign(target, node.MemberName, node.Operator, rightValue, ctx);
    }
}
