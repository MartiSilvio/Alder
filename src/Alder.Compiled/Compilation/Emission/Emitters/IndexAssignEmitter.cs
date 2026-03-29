using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class IndexAssignEmitter : INodeEmitter<BoundIndexAssignExpr>
{
    public LinqExpression Emit(BoundIndexAssignExpr node, EmissionContext ctx)
    {
        return LinqExpression.Call(
            ApplyIndexAssignMethod,
            ctx.EmitBoxed(node.Target),
            ctx.EmitBoxed(node.Index),
            ctx.EmitBoxed(node.Value),
            ctx.ConfigParam,
            ctx.ContextParam);
    }
}
