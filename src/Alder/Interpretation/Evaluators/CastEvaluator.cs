using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal sealed class CastEvaluator : INodeEvaluator<BoundCastExpr>
{
    public object? Evaluate(BoundCastExpr node, EvaluationContext ctx)
    {
        var value = ctx.Evaluate(node.Expression);
        return TypeHelpers.ExplicitCast(value, node.TargetType, node.SourceStaticType, ctx.IsChecked);
    }
}
