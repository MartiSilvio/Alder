using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

internal sealed class LambdaEvaluator : INodeEvaluator<BoundLambdaExpr>
{
    public object? Evaluate(BoundLambdaExpr node, EvaluationContext ctx)
    {
        return new LambdaValue(node.Parameters.ToList(), node.Body, ctx.Context, ctx.Config);
    }
}
