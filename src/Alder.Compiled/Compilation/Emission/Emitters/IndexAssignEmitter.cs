using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.IndexAssignment)]
internal static class IndexAssignEmitter
{
    public static LinqExpression Emit(BoundIndexAssignExpr node, EmissionContext ctx)
    {
        return LinqExpression.Call(
            ApplyIndexAssignMethod,
            ctx.EmitBoxed(node.Target),
            ctx.EmitBoxed(node.Index),
            ctx.EmitBoxed(node.Value),
            ctx.ContextParam);
    }
}
