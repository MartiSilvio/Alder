using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.IndexCompoundAssignment)]
internal static class IndexCompoundAssignEvaluator
{
    public static object? Evaluate(BoundIndexCompoundAssignExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var target = ctx.Evaluate(node.Target, ct);
        var index = ctx.Evaluate(node.Index, ct);
        var rightValue = ctx.Evaluate(node.Value, ct);
        return AssignmentRuntime.ApplyIndexCompoundAssign(target, index, node.Operator, rightValue, ctx);
    }
}
