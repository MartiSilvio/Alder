using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.AsOperator)]
internal static class AsEmitter
{
    public static LinqExpression Emit(BoundAsExpr node, EmissionContext ctx)
    {
        if (!node.TargetType.IsValueType)
        {
            var operand = ctx.EmitBoxed(node.Expression);
            return LinqExpression.TypeAs(operand, node.TargetType);
        }

        return LinqExpression.Call(
            TryAsMethod,
            ctx.EmitBoxed(node.Expression),
            LinqExpression.Constant(node.TargetType, typeof(Type)));
    }
}
