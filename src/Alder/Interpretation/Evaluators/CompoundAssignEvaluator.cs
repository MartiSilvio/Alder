using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal sealed class CompoundAssignEvaluator : INodeEvaluator<BoundCompoundAssignExpr>
{
    public object? Evaluate(BoundCompoundAssignExpr node, EvaluationContext ctx)
    {
        var rightValue = ctx.Evaluate(node.Value);
        return AssignmentRuntime.ApplyCompoundAssign(
            node.Name,
            node.Operator,
            rightValue,
            ctx.Context,
            ctx.Config,
            ctx.IsChecked);
    }
}
