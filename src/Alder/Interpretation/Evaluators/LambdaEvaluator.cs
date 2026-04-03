using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.Lambda)]
internal static class LambdaEvaluator
{
    public static object? Evaluate(BoundLambdaExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        return new LambdaValue(node, ctx.Context);
    }
}
