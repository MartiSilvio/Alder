using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(AwaitExpr))]
internal static class AwaitBinder
{
    public static BoundExpr Bind(AwaitExpr expr, BindingContext context, BinderContext binder)
    {
        // §12.9.8.1: an await_expression shall not occur inside the block of a lock_statement
        if (binder.Includes(BinderFlags.InLockBody))
            throw new AlderException(DiagnosticDescriptors.AwaitInLockBody);

        var operand = binder.Bind(expr.Operand, context);
        var resultType = InferAwaitResultType(operand.StaticType.ClrType);
        return new BoundAwaitExpr(operand, resultType);
    }

    private static BoundType InferAwaitResultType(Type? operandType)
    {
        if (operandType == null || operandType == typeof(object))
            return BoundType.Unknown;

        if (operandType == typeof(Task))
            return BoundType.Void;

        if (operandType == typeof(ValueTask))
            return BoundType.Void;

        if (operandType.IsGenericType)
        {
            var def = operandType.GetGenericTypeDefinition();
            if (def == typeof(Task<>) ||
                def == typeof(ValueTask<>))
            {
                return new BoundType(operandType.GetGenericArguments()[0]);
            }
        }

        return BoundType.Unknown;
    }
}
