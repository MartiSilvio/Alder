using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal sealed class IndexCompoundAssignEvaluator : INodeEvaluator<BoundIndexCompoundAssignExpr>
{
    public object? Evaluate(BoundIndexCompoundAssignExpr node, EvaluationContext ctx)
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
