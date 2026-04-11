using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.MemberAssignment)]
internal static class MemberAssignEvaluator
{
    public static object? Evaluate(BoundMemberAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = ctx.Evaluate(node.Target, ct);
        var value = ctx.Evaluate(node.Value, ct);

        if (node.ResolvedMember is PropertyInfo { CanWrite: true } or FieldInfo { IsInitOnly: false }
            && target != null && node.DeclaringType is { IsValueType: false })
        {
            return MemberAccess.SetResolvedMember(node.ResolvedMember, target, node.MemberName, value, ctx.Context);
        }

        return AssignmentRuntime.ApplyMemberAssign(target, node.MemberName, value, ctx);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundMemberAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = await ctx.EvaluateAsync(node.Target, ct);
        var value = await ctx.EvaluateAsync(node.Value, ct);

        if (node.ResolvedMember is PropertyInfo { CanWrite: true } or FieldInfo { IsInitOnly: false }
            && target != null && node.DeclaringType is { IsValueType: false })
        {
            return MemberAccess.SetResolvedMember(node.ResolvedMember, target, node.MemberName, value, ctx.Context);
        }

        return AssignmentRuntime.ApplyMemberAssign(target, node.MemberName, value, ctx);
    }
}
