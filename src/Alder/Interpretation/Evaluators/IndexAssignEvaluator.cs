using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.IndexAssignment)]
internal static class IndexAssignEvaluator
{
    public static object? Evaluate(BoundIndexAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = ctx.Evaluate(node.Target, ct);
        var index = ctx.Evaluate(node.Index, ct);
        var value = ctx.Evaluate(node.Value, ct);
        return AssignmentRuntime.ApplyIndexAssign(target, index, value, ctx);
    }
}
