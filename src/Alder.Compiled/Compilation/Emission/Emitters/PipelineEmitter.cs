using System.Linq.Expressions;
using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class PipelineEmitter : INodeEmitter<BoundPipelineExpr>
{
    public Expression Emit(BoundPipelineExpr node, EmissionContext ctx)
    {
        if (node.Right is BoundIdentifierExpr rightIdentifier)
        {
            return LinqExpression.Call(
                InvokePipelineIdentifierMethod,
                EmitHelpers.AsObject(ctx.Emit(node.Left)),
                LinqExpression.Constant(rightIdentifier.Name),
                ctx.ContextParam,
                ctx.ConfigParam,
                ctx.CancellationTokenParam);
        }

        return LinqExpression.Call(
            InvokePipelineMethod,
            EmitHelpers.AsObject(ctx.Emit(node.Left)),
            EmitHelpers.AsObject(ctx.Emit(node.Right)),
            ctx.ContextParam,
            ctx.ConfigParam,
            ctx.CancellationTokenParam);
    }
}
