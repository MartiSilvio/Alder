using Alder.Binding.BoundNodes;
using Alder.Runtime;
using Alder.Runtime.Extensions;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal sealed class PipelineEvaluator : INodeEvaluator<BoundPipelineExpr>
{
    public object? Evaluate(BoundPipelineExpr node, EvaluationContext ctx)
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
