using System.Linq.Expressions;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class CastEmitter : INodeEmitter<BoundCastExpr>
{
    public Expression Emit(BoundCastExpr node, EmissionContext ctx)
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
                    ? Expression.ConvertChecked(operand, targetType)
                    : Expression.Convert(operand, targetType);
            }

            if (!targetType.IsValueType && sourceType != typeof(LambdaValue))
            {
                var operand = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Expression), sourceType);
                return Expression.Convert(operand, targetType);
            }
        }

        return Expression.Call(
            ExplicitCastMethod,
            EmitHelpers.AsObject(ctx.Emit(node.Expression)),
            Expression.Constant(node.TargetType, typeof(Type)),
            node.SourceStaticType == null
                ? Expression.Constant(null, typeof(Type))
                : Expression.Constant(node.SourceStaticType, typeof(Type)),
            Expression.Constant(ctx.IsChecked));
    }
}
