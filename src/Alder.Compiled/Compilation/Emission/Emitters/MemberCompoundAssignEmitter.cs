using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.MemberCompoundAssignment)]
internal static class MemberCompoundAssignEmitter
{
    public static LinqExpression Emit(BoundMemberCompoundAssignExpr node, EmissionContext ctx)
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
