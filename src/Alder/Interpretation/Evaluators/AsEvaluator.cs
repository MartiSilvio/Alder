using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal sealed class AsEvaluator : INodeEvaluator<BoundAsExpr>
{
    public object? Evaluate(BoundAsExpr node, EvaluationContext ctx)
    {
        var value = ctx.Evaluate(node.Expression);
        return TypeHelpers.TryAs(value, node.TargetType);
    }
}
