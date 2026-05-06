using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.AsOperator)]
internal static class AsEvaluator
{
    public static object? Evaluate(BoundAsExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = ctx.Evaluate(node.Expression, ct);
        return TypeHelpers.TryAs(value, node.TargetType);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundAsExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = await ctx.EvaluateAsync(node.Expression, ct);
        return TypeHelpers.TryAs(value, node.TargetType);
    }
}
