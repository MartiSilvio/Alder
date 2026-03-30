using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.IndexCompoundAssignment)]
internal static class IndexCompoundAssignEvaluator
{
    public static object? Evaluate(BoundIndexCompoundAssignExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);
        var index = ctx.Evaluate(node.Index);
        var rightValue = ctx.Evaluate(node.Value);
        return AssignmentRuntime.ApplyIndexCompoundAssign(
            target,
            index,
            node.Operator,
            rightValue,
            ctx.Config,
            ctx.Context,
            ctx.IsChecked);
    }
}
