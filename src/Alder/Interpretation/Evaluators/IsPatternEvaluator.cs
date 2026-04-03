using Alder.Binding;
using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.IsPatternExpression)]
internal static class IsPatternEvaluator
{
    public static object? Evaluate(BoundIsPatternExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var value = ctx.Evaluate(node.Expression, ct);
        return ctx.MatchPattern(value, node.Pattern, ct);
    }
}
