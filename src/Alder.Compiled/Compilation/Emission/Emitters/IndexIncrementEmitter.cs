using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class IndexIncrementEmitter : INodeEmitter<BoundIndexIncrementExpr>
{
    public LinqExpression Emit(BoundIndexIncrementExpr node, EmissionContext ctx)
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
