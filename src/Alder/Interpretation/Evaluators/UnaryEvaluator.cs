using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.UnaryOperator)]
internal static class UnaryEvaluator
{
    public static object? Evaluate(BoundUnaryExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var operand = ctx.Evaluate(node.Operand, ct);

        if (node.PromotedType is { } promoted && operand != null
            && operand.GetType() == node.Operand.StaticType.ClrType)
        {
            return node.Operator switch
            {
                TokenType.Minus => NumericDispatch.Negate(operand, promoted, ctx.IsChecked),
                TokenType.Plus => NumericDispatch.UnaryPlus(operand, promoted),
                TokenType.Tilde => NumericDispatch.BitwiseNot(operand, promoted),
                TokenType.Bang => Operators.LogicalNot(operand),
                _ => throw new BindingNotSupportedException(
                    $"Bound unary operator '{node.Operator}' is not implemented")
            };
        }

        return node.Operator switch
        {
            TokenType.Minus => Operators.Negate(operand, ctx.IsChecked),
            TokenType.Plus => Operators.UnaryPlus(operand),
            TokenType.Bang => Operators.LogicalNot(operand),
            TokenType.Tilde => Operators.BitwiseNot(operand),
            _ => throw new BindingNotSupportedException(
                $"Bound unary operator '{node.Operator}' is not implemented")
        };
    }

    public static async ValueTask<object?> EvaluateAsync(BoundUnaryExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var operand = await ctx.EvaluateAsync(node.Operand, ct);

        if (node.PromotedType is { } promoted && operand != null
            && operand.GetType() == node.Operand.StaticType.ClrType)
        {
            return node.Operator switch
            {
                TokenType.Minus => NumericDispatch.Negate(operand, promoted, ctx.IsChecked),
                TokenType.Plus => NumericDispatch.UnaryPlus(operand, promoted),
                TokenType.Tilde => NumericDispatch.BitwiseNot(operand, promoted),
                TokenType.Bang => Operators.LogicalNot(operand),
                _ => throw new BindingNotSupportedException(
                    $"Bound unary operator '{node.Operator}' is not implemented")
            };
        }

        return node.Operator switch
        {
            TokenType.Minus => Operators.Negate(operand, ctx.IsChecked),
            TokenType.Plus => Operators.UnaryPlus(operand),
            TokenType.Bang => Operators.LogicalNot(operand),
            TokenType.Tilde => Operators.BitwiseNot(operand),
            _ => throw new BindingNotSupportedException(
                $"Bound unary operator '{node.Operator}' is not implemented")
        };
    }
}
