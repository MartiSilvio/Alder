using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.MemberIncrement)]
internal static class MemberIncrementEmitter
{
    public static LinqExpression Emit(BoundMemberIncrementExpr node, EmissionContext ctx)
    {
        return LinqExpression.Call(
            ApplyMemberIncrementMethod,
            ctx.EmitBoxed(node.Target),
            LinqExpression.Constant(node.MemberName),
            LinqExpression.Constant(node.IsIncrement),
            LinqExpression.Constant(node.IsPrefix),
            ctx.ContextParam,
            LinqExpression.Constant(ctx.IsChecked));
    }
}
