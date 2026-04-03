using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.CheckedExpression)]
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

    public static async ValueTask<object?> EvaluateAsync(BoundCheckedExpr node, EvaluationContext ctx)
    {
        var previous = ctx.IsChecked;
        ctx.IsChecked = node.IsChecked;
        try
        {
            return await ctx.EvaluateAsync(node.Expression);
        }
        finally
        {
            ctx.IsChecked = previous;
        }
    }
}
