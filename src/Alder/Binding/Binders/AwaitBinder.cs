using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(AwaitExpr))]
internal static class AwaitBinder
{
    public static BoundExpr Bind(AwaitExpr expr, BindingContext context, BinderContext binder)
    {
        var operand = binder.Bind(expr.Operand, context);
        var resultType = InferAwaitResultType(operand.StaticType.ClrType);
        return new BoundAwaitExpr(operand, resultType);
    }

    private static BoundType InferAwaitResultType(Type? operandType)
    {
        if (operandType == null || operandType == typeof(object))
            return BoundType.Unknown;

        if (operandType == typeof(System.Threading.Tasks.Task))
            return BoundType.Void;

        if (operandType == typeof(System.Threading.Tasks.ValueTask))
            return BoundType.Void;

        if (operandType.IsGenericType)
        {
            var def = operandType.GetGenericTypeDefinition();
            if (def == typeof(System.Threading.Tasks.Task<>) ||
                def == typeof(System.Threading.Tasks.ValueTask<>))
            {
                return new BoundType(operandType.GetGenericArguments()[0]);
            }
        }

        return BoundType.Unknown;
    }
}
