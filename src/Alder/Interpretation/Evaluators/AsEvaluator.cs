using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.AsOperator)]
internal static class AsEvaluator
{
    public static object? Evaluate(BoundAsExpr node, EvaluationContext ctx)
    {
        var value = ctx.Evaluate(node.Expression);
        return TypeHelpers.TryAs(value, node.TargetType);
    }
}
