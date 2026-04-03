using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Extensions;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.ChainedComparisonOperator)]
internal static class ChainedComparisonEvaluator
{
    public static object? Evaluate(BoundChainedComparisonExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var previousValue = ctx.Evaluate(node.Operands[0], ct);

        for (var i = 0; i < node.Operators.Length; i++)
        {
            var nextValue = ctx.Evaluate(node.Operands[i + 1], ct);
            if (!ChainedComparisonHelper.PerformComparison(
                    previousValue,
                    nextValue,
                    node.Operators[i],
                    ctx.Context.Config.StringComparison))
            {
                return BoxedConstants.False;
            }

            previousValue = nextValue;
        }

        return BoxedConstants.True;
    }

    public static async ValueTask<object?> EvaluateAsync(BoundChainedComparisonExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var previousValue = await ctx.EvaluateAsync(node.Operands[0], ct);

        for (var i = 0; i < node.Operators.Length; i++)
        {
            var nextValue = await ctx.EvaluateAsync(node.Operands[i + 1], ct);
            if (!ChainedComparisonHelper.PerformComparison(
                    previousValue,
                    nextValue,
                    node.Operators[i],
                    ctx.Context.Config.StringComparison))
            {
                return BoxedConstants.False;
            }

            previousValue = nextValue;
        }

        return BoxedConstants.True;
    }
}
