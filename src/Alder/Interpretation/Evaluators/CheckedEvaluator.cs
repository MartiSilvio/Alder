using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal static class CheckedEvaluator
{
    public static object? Evaluate(BoundCheckedExpr node, EvaluationContext ctx)
    {
        var previous = ctx.IsChecked;
        ctx.IsChecked = node.IsChecked;
        try
        {
            return ctx.Evaluate(node.Expression);
        }
        finally
        {
            ctx.IsChecked = previous;
        }
    }
}
