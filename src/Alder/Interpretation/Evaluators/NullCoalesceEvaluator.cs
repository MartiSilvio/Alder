using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.NullCoalescingOperator)]
internal static class NullCoalesceEvaluator
{
    public static object? Evaluate(BoundNullCoalesceExpr node, EvaluationContext ctx)
    {
        var left = ctx.Evaluate(node.Left);
        return left ?? ctx.Evaluate(node.Right);
    }
}
