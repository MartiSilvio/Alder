using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.IndexIncrement)]
internal static class IndexIncrementEmitter
{
    public static LinqExpression Emit(BoundIndexIncrementExpr node, EmissionContext ctx)
    {
        return LinqExpression.Call(
            ApplyIndexIncrementMethod,
            ctx.EmitBoxed(node.Target),
            ctx.EmitBoxed(node.Index),
            LinqExpression.Constant(node.IsIncrement),
            LinqExpression.Constant(node.IsPrefix),
            ctx.ContextParam,
            LinqExpression.Constant(ctx.IsChecked));
    }
}
