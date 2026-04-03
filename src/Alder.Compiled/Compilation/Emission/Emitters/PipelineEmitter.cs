using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class PipelineEmitter : INodeEmitter<BoundPipelineExpr>
{
    public LinqExpression Emit(BoundPipelineExpr node, EmissionContext ctx)
    {
        if (node.Right is BoundIdentifierExpr rightIdentifier)
        {
            return LinqExpression.Call(
                InvokePipelineIdentifierMethod,
                ctx.EmitBoxed(node.Left),
                LinqExpression.Constant(rightIdentifier.Name),
                ctx.ContextParam,
                ctx.CancellationTokenParam);
        }

        return LinqExpression.Call(
            InvokePipelineMethod,
            ctx.EmitBoxed(node.Left),
            ctx.EmitBoxed(node.Right),
            ctx.ContextParam,
            ctx.CancellationTokenParam);
    }
}
