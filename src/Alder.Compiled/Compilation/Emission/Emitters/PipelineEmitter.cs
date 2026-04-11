using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.PipelineExpression)]
internal static class PipelineEmitter
{
    public static LinqExpression Emit(BoundPipelineExpr node, EmissionContext ctx)
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
