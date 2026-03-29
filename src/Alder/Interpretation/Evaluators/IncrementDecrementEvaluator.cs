using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal sealed class IncrementDecrementEvaluator : INodeEvaluator<BoundIncrementDecrementExpr>
{
    public object? Evaluate(BoundIncrementDecrementExpr node, EvaluationContext ctx)
    {
        return AssignmentRuntime.ApplyIncrementDecrement(
            node.Name,
            node.Operator == TokenType.PlusPlus,
            node.IsPrefix,
            ctx.Context,
            ctx.Config,
            ctx.IsChecked);
    }
}
