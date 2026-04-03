using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class MemberCompoundAssignEmitter : INodeEmitter<BoundMemberCompoundAssignExpr>
{
    public LinqExpression Emit(BoundMemberCompoundAssignExpr node, EmissionContext ctx)
    {
        return LinqExpression.Call(
            ApplyMemberCompoundAssignMethod,
            ctx.EmitBoxed(node.Target),
            LinqExpression.Constant(node.MemberName),
            LinqExpression.Constant(node.Operator),
            ctx.EmitBoxed(node.Value),
            ctx.ContextParam,
            LinqExpression.Constant(ctx.IsChecked));
    }
}
