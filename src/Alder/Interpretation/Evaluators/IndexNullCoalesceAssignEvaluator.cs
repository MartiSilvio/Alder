using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.IndexNullCoalesceAssignment)]
internal static class IndexNullCoalesceAssignEvaluator
{
    public static object? Evaluate(BoundIndexNullCoalesceAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = ctx.Evaluate(node.Target, ct);
        target = ExecutionRuntime.EnsureIndexTargetNotNull(target);
        var index = ctx.Evaluate(node.Index, ct);
        var currentValue = MemberAccess.GetIndex(target, index, ctx.Context);
        if (currentValue != null)
            return currentValue;
        var newValue = ctx.Evaluate(node.Value, ct);
        MemberAccess.SetIndex(target, index, newValue, ctx.Context);
        return newValue;
    }
}
