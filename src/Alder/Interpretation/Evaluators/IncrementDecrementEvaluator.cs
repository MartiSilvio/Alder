using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.IncrementOperator)]
internal static class IncrementDecrementEvaluator
{
    public static object? Evaluate(BoundIncrementDecrementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        return AssignmentRuntime.ApplyIncrementDecrement(
            node.Name, node.Operator == TokenType.PlusPlus, node.IsPrefix, ctx.Context, ctx.IsChecked);
    }
}
