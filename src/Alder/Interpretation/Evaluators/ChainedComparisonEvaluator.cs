using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Extensions;

namespace Alder.Interpretation.Evaluators;

internal sealed class ChainedComparisonEvaluator : INodeEvaluator<BoundChainedComparisonExpr>
{
    public object? Evaluate(BoundChainedComparisonExpr node, EvaluationContext ctx)
    {
        var previousValue = ctx.Evaluate(node.Operands[0]);

        for (var i = 0; i < node.Operators.Length; i++)
        {
            var nextValue = ctx.Evaluate(node.Operands[i + 1]);
            if (!ChainedComparisonHelper.PerformComparison(
                    previousValue,
                    nextValue,
                    node.Operators[i],
                    ctx.Config.StringComparison))
            {
                return BoxedConstants.False;
            }

            previousValue = nextValue;
        }

        return BoxedConstants.True;
    }
}
