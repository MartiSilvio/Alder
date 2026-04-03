using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class IndexCompoundAssignEmitter : INodeEmitter<BoundIndexCompoundAssignExpr>
{
    public LinqExpression Emit(BoundIndexCompoundAssignExpr node, EmissionContext ctx)
    {
        return LinqExpression.Call(
            ApplyIndexCompoundAssignMethod,
            ctx.EmitBoxed(node.Target),
            ctx.EmitBoxed(node.Index),
            LinqExpression.Constant(node.Operator),
            ctx.EmitBoxed(node.Value),
            ctx.ContextParam,
            LinqExpression.Constant(ctx.IsChecked));
    }
}
