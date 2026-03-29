using System.Linq.Expressions;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class CastEmitter : INodeEmitter<BoundCastExpr>
{
    public LinqExpression Emit(BoundCastExpr node, EmissionContext ctx)
    {
        var sourceType = node.Expression.StaticType.ClrType;
        var targetType = node.TargetType;

        if (sourceType != typeof(object) && !sourceType.IsEnum && !targetType.IsEnum)
        {
            if ((TypeHelpers.IsArithmetic(sourceType) || sourceType == typeof(bool)) &&
                (TypeHelpers.IsArithmetic(targetType) || targetType == typeof(bool)))
            {
                var operand = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Expression), sourceType);
                return ctx.IsChecked
                    ? LinqExpression.ConvertChecked(operand, targetType)
                    : LinqExpression.Convert(operand, targetType);
            }

            if (!targetType.IsValueType && sourceType != typeof(LambdaValue))
            {
                var operand = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Expression), sourceType);
                return LinqExpression.Convert(operand, targetType);
            }
        }

        return LinqExpression.Call(
            ExplicitCastMethod,
            EmitHelpers.AsObject(ctx.Emit(node.Expression)),
            LinqExpression.Constant(node.TargetType, typeof(Type)),
            node.SourceStaticType == null
                ? LinqExpression.Constant(null, typeof(Type))
                : LinqExpression.Constant(node.SourceStaticType, typeof(Type)),
            LinqExpression.Constant(ctx.IsChecked));
    }
}
