using System.Linq.Expressions;
using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class AsEmitter : INodeEmitter<BoundAsExpr>
{
    public LinqExpression Emit(BoundAsExpr node, EmissionContext ctx)
    {
        if (!node.TargetType.IsValueType)
        {
            var operand = EmitHelpers.AsObject(ctx.Emit(node.Expression));
            return LinqExpression.TypeAs(operand, node.TargetType);
        }

        return LinqExpression.Call(
            TryAsMethod,
            EmitHelpers.AsObject(ctx.Emit(node.Expression)),
            LinqExpression.Constant(node.TargetType, typeof(Type)));
    }
}
