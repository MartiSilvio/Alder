using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal sealed class IsPatternEvaluator : INodeEvaluator<BoundIsPatternExpr>
{
    public object? Evaluate(BoundIsPatternExpr node, EvaluationContext ctx)
    {
        var value = ctx.Evaluate(node.Expression);
        return ctx.MatchPattern(value, node.Pattern);
    }
}
