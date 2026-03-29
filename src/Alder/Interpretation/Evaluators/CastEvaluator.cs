using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal static class CastEvaluator
{
    public static object? Evaluate(BoundCastExpr node, EvaluationContext ctx)
    {
        var value = ctx.Evaluate(node.Expression);
        return TypeHelpers.ExplicitCast(value, node.TargetType, node.SourceStaticType, ctx.IsChecked);
    }
}
