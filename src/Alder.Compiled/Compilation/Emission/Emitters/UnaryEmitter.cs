using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Parsing;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.UnaryOperator)]
internal static class UnaryEmitter
{
    public static LinqExpression Emit(BoundUnaryExpr node, EmissionContext ctx)
    {
        if (node.PromotedType is { } promoted)
        {
            var operand = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Operand), node.Operand.StaticType.ClrType);
            if (operand.Type != promoted)
                operand = LinqExpression.Convert(operand, promoted);

            return node.Operator switch
            {
                TokenType.Bang => LinqExpression.Not(operand),
                TokenType.Tilde => LinqExpression.Not(operand),
                TokenType.Minus => ctx.IsChecked ? LinqExpression.NegateChecked(operand) : LinqExpression.Negate(operand),
                TokenType.Plus => operand,
                _ => throw new BindingNotSupportedException($"Unsupported bound unary operator '{node.Operator}'")
            };
        }

        var boxed = ctx.EmitBoxed(node.Operand);
        return node.Operator switch
        {
            TokenType.Minus => LinqExpression.Call(NegateMethod, boxed, LinqExpression.Constant(ctx.IsChecked)),
            TokenType.Plus => LinqExpression.Call(UnaryPlusMethod, boxed),
            TokenType.Bang => LinqExpression.Call(LogicalNotMethod, boxed),
            TokenType.Tilde => LinqExpression.Call(BitwiseNotMethod, boxed),
            _ => throw new BindingNotSupportedException($"Unsupported bound unary operator '{node.Operator}'")
        };
    }
}
