using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class UnaryEmitter : INodeEmitter<BoundUnaryExpr>
{
    public LinqExpression Emit(BoundUnaryExpr node, EmissionContext ctx)
    {
        if (node.PromotedType is { } promoted && !ctx.IsChecked)
        {
            var operand = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Operand), node.Operand.StaticType.ClrType);
            if (operand.Type != promoted)
                operand = LinqExpression.Convert(operand, promoted);

            return node.Operator switch
            {
                TokenType.Bang => LinqExpression.Not(operand),
                TokenType.Tilde => LinqExpression.Not(operand),
                TokenType.Minus => LinqExpression.Negate(operand),
                TokenType.Plus => operand,
                _ => throw new BindingNotSupportedException($"Unsupported bound unary operator '{node.Operator}'")
            };
        }

        var boxed = EmitHelpers.AsObject(ctx.Emit(node.Operand));
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
