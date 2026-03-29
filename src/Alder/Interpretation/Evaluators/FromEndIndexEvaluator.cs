using Alder.Binding.BoundNodes;

namespace Alder.Interpretation.Evaluators;

internal static class FromEndIndexEvaluator
{
    public static object? Evaluate(BoundIndexFromEndExpr node, EvaluationContext ctx)
    {
        return new Index(Convert.ToInt32(ctx.Evaluate(node.Operand)), fromEnd: true);
    }
}
