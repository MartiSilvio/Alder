using Alder.Binding.BoundNodes;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class NullCoalesceEmitter : INodeEmitter<BoundNullCoalesceExpr>
{
    public LinqExpression Emit(BoundNullCoalesceExpr node, EmissionContext ctx)
    {
        var leftType = node.Left.StaticType.ClrType;
        var rightType = node.Right.StaticType.ClrType;

        if (leftType != typeof(object) && rightType != typeof(object)
            && (leftType.IsClass || leftType.IsInterface || Nullable.GetUnderlyingType(leftType) != null))
        {
            var left = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Left), leftType);
            var right = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Right), rightType);

            if (left.Type != right.Type)
            {
                var commonType = node.StaticType.ClrType;
                if (commonType != typeof(object))
                {
                    if (right.Type != commonType)
                        right = LinqExpression.Convert(right, commonType);
                }
                else
                {
                    left = EmitHelpers.AsObject(left);
                    right = EmitHelpers.AsObject(right);
                }
            }

            return LinqExpression.Coalesce(left, right);
        }

        var leftVar = LinqExpression.Variable(typeof(object), "coalesceLeft");
        return LinqExpression.Block(
            typeof(object),
            [leftVar],
            LinqExpression.Assign(leftVar, ctx.EmitBoxed(node.Left)),
            LinqExpression.Condition(
                LinqExpression.Equal(leftVar, LinqExpression.Constant(null, typeof(object))),
                ctx.EmitBoxed(node.Right),
                leftVar));
    }
}
