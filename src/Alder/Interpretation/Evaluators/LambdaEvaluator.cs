using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.Lambda)]
internal static class LambdaEvaluator
{
    public static object? Evaluate(BoundLambdaExpr node, EvaluationContext ctx)
    {
        return new LambdaValue(node.Parameters.ToList(), node.Body, ctx.Context, ctx.Config);
    }
}
