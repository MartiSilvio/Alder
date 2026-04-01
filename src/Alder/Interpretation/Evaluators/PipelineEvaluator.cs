using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime.Extensions;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.PipelineExpression)]
internal static class PipelineEvaluator
{
    public static object? Evaluate(BoundPipelineExpr node, EvaluationContext ctx)
    {
        var left = ctx.Evaluate(node.Left);

        if (node.Right is BoundIdentifierExpr rightIdentifier)
        {
            return IdentifierRuntime.InvokePipelineIdentifier(
                left,
                rightIdentifier.Name,
                ctx.Context,
                ctx.Config,
                ctx.CancellationToken);
        }

        var right = ctx.Evaluate(node.Right);
        return PipelineOperator.InvokePipeline(left, right, ctx.Context, ctx.Config, ctx.CancellationToken);
    }
}
