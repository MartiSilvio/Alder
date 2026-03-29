using Alder.Binding.BoundNodes;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal static class IndexIncrementEvaluator
{
    public static object? Evaluate(BoundIndexIncrementExpr node, EvaluationContext ctx)
    {
        var target = ctx.Evaluate(node.Target);
        var index = ctx.Evaluate(node.Index);
        return AssignmentRuntime.ApplyIndexIncrement(
            target,
            index,
            node.IsIncrement,
            node.IsPrefix,
            ctx.Config,
            ctx.Context,
            ctx.IsChecked);
    }
}
