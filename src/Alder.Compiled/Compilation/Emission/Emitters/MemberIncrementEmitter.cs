using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class MemberIncrementEmitter : INodeEmitter<BoundMemberIncrementExpr>
{
    public LinqExpression Emit(BoundMemberIncrementExpr node, EmissionContext ctx)
    {
        return LinqExpression.Call(
            ApplyMemberIncrementMethod,
            ctx.EmitBoxed(node.Target),
            LinqExpression.Constant(node.MemberName),
            LinqExpression.Constant(node.IsIncrement),
            LinqExpression.Constant(node.IsPrefix),
            ctx.ConfigParam,
            ctx.ContextParam,
            LinqExpression.Constant(ctx.IsChecked));
    }
}
