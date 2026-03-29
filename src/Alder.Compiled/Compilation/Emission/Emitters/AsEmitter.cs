using System.Linq.Expressions;
using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class AsEmitter : INodeEmitter<BoundAsExpr>
{
    public Expression Emit(BoundAsExpr node, EmissionContext ctx)
    {
        if (!node.TargetType.IsValueType)
        {
            var operand = EmitHelpers.AsObject(ctx.Emit(node.Expression));
            return Expression.TypeAs(operand, node.TargetType);
        }

        return Expression.Call(
            TryAsMethod,
            EmitHelpers.AsObject(ctx.Emit(node.Expression)),
            Expression.Constant(node.TargetType, typeof(Type)));
    }
}
