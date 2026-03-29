using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Runtime.Extensions;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

internal sealed class BinaryEvaluator : INodeEvaluator<BoundBinaryExpr>
{
    public object? Evaluate(BoundBinaryExpr node, EvaluationContext ctx)
    {
        var chain = new List<BoundBinaryExpr>();
        BoundExpr leftmost = node;
        while (leftmost is BoundBinaryExpr b)
        {
            chain.Add(b);
            leftmost = b.Left;
        }

        for (var i = chain.Count - 1; i > 0; i--)
            ctx.Tracer?.Push(chain[i]);

        var result = ctx.Evaluate(leftmost);
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            result = EvaluateBinarySingle(chain[i], result, ctx);
            if (i > 0)
                ctx.Tracer?.Pop(result);
        }
        return result;
    }

    private static object? EvaluateBinarySingle(BoundBinaryExpr binary, object? left, EvaluationContext ctx)
    {
        var right = ctx.Evaluate(binary.Right);

        if (binary.PromotedType is { } promoted && left != null && right != null
            && left.GetType() == binary.Left.StaticType.ClrType
            && right.GetType() == binary.Right.StaticType.ClrType)
        {
            if (IsNaN(left) || IsNaN(right))
                return binary.Operator == TokenType.BangEqual ? BoxedConstants.True : BoxedConstants.False;

            return binary.Operator switch
            {
                TokenType.Plus => NumericDispatch.Add(left, right, promoted, ctx.IsChecked),
                TokenType.Minus => NumericDispatch.Subtract(left, right, promoted, ctx.IsChecked),
                TokenType.Star => NumericDispatch.Multiply(left, right, promoted, ctx.IsChecked),
                TokenType.Slash => NumericDispatch.Divide(left, right, promoted),
                TokenType.Percent => NumericDispatch.Modulo(left, right, promoted),
                TokenType.EqualEqual => NumericDispatch.Compare(left, right, promoted) == 0
                    ? BoxedConstants.True : BoxedConstants.False,
                TokenType.BangEqual => NumericDispatch.Compare(left, right, promoted) != 0
                    ? BoxedConstants.True : BoxedConstants.False,
                TokenType.Less => NumericDispatch.Compare(left, right, promoted) < 0
                    ? BoxedConstants.True : BoxedConstants.False,
                TokenType.LessEqual => NumericDispatch.Compare(left, right, promoted) <= 0
                    ? BoxedConstants.True : BoxedConstants.False,
                TokenType.Greater => NumericDispatch.Compare(left, right, promoted) > 0
                    ? BoxedConstants.True : BoxedConstants.False,
                TokenType.GreaterEqual => NumericDispatch.Compare(left, right, promoted) >= 0
                    ? BoxedConstants.True : BoxedConstants.False,
                TokenType.Amp => NumericDispatch.BitwiseAnd(left, right, promoted),
                TokenType.Pipe => NumericDispatch.BitwiseOr(left, right, promoted),
                TokenType.Caret => NumericDispatch.BitwiseXor(left, right, promoted),
                TokenType.LessLess => NumericDispatch.LeftShift(left, right),
                TokenType.GreaterGreater => NumericDispatch.RightShift(left, right),
                _ => EvaluateBinaryFallback(binary, left, right, ctx)
            };
        }

        (left, right) = NumericPromotionRuntime.ApplyConstantNumericPromotion(
            left,
            binary.Left.Kind == BoundNodeKind.Literal,
            right,
            binary.Right.Kind == BoundNodeKind.Literal);

        return EvaluateBinaryFallback(binary, left, right, ctx);
    }

    private static object? EvaluateBinaryFallback(BoundBinaryExpr binary, object? left, object? right, EvaluationContext ctx)
    {
        return binary.Operator switch
        {
            TokenType.Plus => Operators.Add(left, right, ctx.Config, ctx.Context, ctx.IsChecked,
                isStringContext: binary.Left.StaticType.ClrType == typeof(string) || binary.Right.StaticType.ClrType == typeof(string)),
            TokenType.Minus => Operators.Subtract(left, right, ctx.IsChecked),
            TokenType.Star => Operators.Multiply(left, right, ctx.Config.LanguageMode, ctx.IsChecked),
            TokenType.Slash => Operators.Divide(left, right),
            TokenType.Percent => Operators.Modulo(left, right),
            TokenType.EqualEqual => Operators.Equals(left, right),
            TokenType.BangEqual => Operators.NotEquals(left, right),
            TokenType.EqualEqualEqual => Operators.StrictEquals(left, right),
            TokenType.BangEqualEqual => Operators.StrictNotEquals(left, right),
            TokenType.Less => Operators.LessThan(left, right, ctx.Config.StringComparison),
            TokenType.LessEqual => Operators.LessThanOrEqual(left, right, ctx.Config.StringComparison),
            TokenType.Greater => Operators.GreaterThan(left, right, ctx.Config.StringComparison),
            TokenType.GreaterEqual => Operators.GreaterThanOrEqual(left, right, ctx.Config.StringComparison),
            TokenType.Amp => Operators.BitwiseAnd(left, right),
            TokenType.Pipe => Operators.BitwiseOr(left, right),
            TokenType.Caret => Operators.BitwiseXor(left, right),
            TokenType.LessLess => Operators.LeftShift(left, right),
            TokenType.GreaterGreater => Operators.RightShift(left, right),
            TokenType.GreaterGreaterGreater => Operators.UnsignedRightShift(left, right),
            TokenType.StarStar => Operators.Power(left, right),
            TokenType.In => Operators.InOperator(left, right),
            TokenType.Like => Operators.Like(left, right, ctx.Config.StringComparison),
            TokenType.EqualTilde => Operators.RegexMatch(left, right),
            TokenType.BangTilde => Operators.RegexNotMatch(left, right),
            TokenType.LessEqualGreater => Operators.Spaceship(left, right),
            _ => throw new BindingNotSupportedException(
                $"Bound binary operator '{binary.Operator}' is not implemented")
        };
    }

    private static bool IsNaN(object value) => value is double d && double.IsNaN(d) || value is float f && float.IsNaN(f);
}
