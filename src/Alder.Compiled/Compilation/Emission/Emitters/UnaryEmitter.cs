using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class UnaryEmitter : INodeEmitter<BoundUnaryExpr>
{
    public Expression Emit(BoundUnaryExpr node, EmissionContext ctx)
    {
        if (node.PromotedType is { } promoted && !ctx.IsChecked)
        {
            var operand = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Operand), node.Operand.StaticType.ClrType);
            if (operand.Type != promoted)
                operand = Expression.Convert(operand, promoted);

            return node.Operator switch
            {
                TokenType.Bang => Expression.Not(operand),
                TokenType.Tilde => Expression.Not(operand),
                TokenType.Minus => Expression.Negate(operand),
                TokenType.Plus => operand,
                _ => throw new BindingNotSupportedException($"Unsupported bound unary operator '{node.Operator}'")
            };
        }

        var boxed = EmitHelpers.AsObject(ctx.Emit(node.Operand));
        return node.Operator switch
        {
            TokenType.Minus => Expression.Call(NegateMethod, boxed, Expression.Constant(ctx.IsChecked)),
            TokenType.Plus => Expression.Call(UnaryPlusMethod, boxed),
            TokenType.Bang => Expression.Call(LogicalNotMethod, boxed),
            TokenType.Tilde => Expression.Call(BitwiseNotMethod, boxed),
            _ => throw new BindingNotSupportedException($"Unsupported bound unary operator '{node.Operator}'")
        };
    }
}
