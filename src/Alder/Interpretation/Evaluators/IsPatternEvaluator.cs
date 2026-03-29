using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal static class IsPatternEvaluator
{
    public static object? Evaluate(BoundIsPatternExpr node, EvaluationContext ctx)
    {
        var value = ctx.Evaluate(node.Expression);
        return ctx.MatchPattern(value, node.Pattern);
    }
}
