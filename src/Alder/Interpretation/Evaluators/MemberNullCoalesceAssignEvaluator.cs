using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.MemberNullCoalesceAssignment)]
internal static class MemberNullCoalesceAssignEvaluator
{
    public static object? Evaluate(BoundMemberNullCoalesceAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = ctx.Evaluate(node.Target, ct);
        target = ExecutionRuntime.EnsureMemberTargetNotNull(target, node.MemberName);
        var currentValue = MemberAccess.GetMember(target, node.MemberName, false, ctx.Context);
        if (currentValue != null)
            return currentValue;
        var newValue = ctx.Evaluate(node.Value, ct);
        MemberAccess.SetMember(target, node.MemberName, newValue, ctx.Context);
        return newValue;
    }
}
