using System.Reflection;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal sealed class MemberAssignEvaluator : INodeEvaluator<BoundMemberAssignExpr>
{
    public object? Evaluate(BoundMemberAssignExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);
        var value = ctx.Evaluate(node.Value);

        if (node.ResolvedMember is PropertyInfo property && property.CanWrite
            && target != null && node.DeclaringType is { IsValueType: false })
        {
            property.SetValue(target, value);
            return value;
        }

        if (node.ResolvedMember is FieldInfo field && !field.IsInitOnly
            && target != null && node.DeclaringType is { IsValueType: false })
        {
            field.SetValue(target, value);
            return value;
        }

        return AssignmentRuntime.ApplyMemberAssign(target, node.MemberName, value, ctx.Config, ctx.Context);
    }
}
