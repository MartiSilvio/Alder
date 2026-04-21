using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.BinaryOperator)]
internal static class BinaryEvaluator
{
    public static object? Evaluate(BoundBinaryExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        if (node.Left is not BoundBinaryExpr)
            return EvaluateBinarySingle(node, ctx.Evaluate(node.Left, ct), ctx, ct);

        var chain = new List<BoundBinaryExpr>();
        BoundExpr leftmost = node;
        while (leftmost is BoundBinaryExpr b)
        {
            chain.Add(b);
            leftmost = b.Left;
        }

        for (var i = chain.Count - 1; i > 0; i--)
            ctx.Tracer?.Push(chain[i]);

        var result = ctx.Evaluate(leftmost, ct);
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            result = EvaluateBinarySingle(chain[i], result, ctx, ct);
            if (i > 0)
                ctx.Tracer?.Pop(result);
        }
        return result;
    }

    private static object? EvaluateBinarySingle(BoundBinaryExpr binary, object? left, EvaluationContext ctx, CancellationToken ct)
    {
        var right = ctx.Evaluate(binary.Right, ct);

        if (binary.PromotedType is { } promoted && left != null && right != null
            && left.GetType() == binary.Left.StaticType.ClrType
            && right.GetType() == binary.Right.StaticType.ClrType)
        {
            if (promoted == typeof(int) && left is int li && right is int ri)
                return EvaluateInt32Fast(binary.Operator, li, ri, ctx.IsChecked);

            if (promoted == typeof(double) && left is double ld && right is double rd)
                return EvaluateDoubleFast(binary.Operator, ld, rd);

            if ((IsNaN(left) || IsNaN(right)) &&
                binary.Operator is TokenType.EqualEqual or TokenType.BangEqual or
                TokenType.Less or TokenType.LessEqual or TokenType.Greater or TokenType.GreaterEqual)
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
                _ => EvaluateBinaryFallback(binary, left, right, ctx, ct)
            };
        }

        (left, right) = NumericPromotionRuntime.ApplyConstantNumericPromotion(
            left,
            binary.Left.Kind == BoundNodeKind.Literal,
            right,
            binary.Right.Kind == BoundNodeKind.Literal);

        return EvaluateBinaryFallback(binary, left, right, ctx, ct);
    }

    private static object? EvaluateBinaryFallback(BoundBinaryExpr binary, object? left, object? right, EvaluationContext ctx, CancellationToken ct)
    {
        return binary.Operator switch
        {
            TokenType.Plus => Operators.Add(left, right, ctx.Context.Config, ctx.IsChecked,
                isStringContext: binary.Left.StaticType.ClrType == typeof(string) || binary.Right.StaticType.ClrType == typeof(string)),
            TokenType.Minus => Operators.Subtract(left, right, ctx.IsChecked),
            TokenType.Star => Operators.Multiply(left, right, ctx.IsChecked),
            TokenType.Slash => Operators.Divide(left, right),
            TokenType.Percent => Operators.Modulo(left, right),
            TokenType.EqualEqual => Operators.Equals(left, right),
            TokenType.BangEqual => Operators.NotEquals(left, right),
            TokenType.EqualEqualEqual => Operators.StrictEquals(left, right),
            TokenType.BangEqualEqual => Operators.StrictNotEquals(left, right),
            TokenType.Less => Operators.LessThan(left, right, ctx.Context.Config.StringComparison),
            TokenType.LessEqual => Operators.LessThanOrEqual(left, right, ctx.Context.Config.StringComparison),
            TokenType.Greater => Operators.GreaterThan(left, right, ctx.Context.Config.StringComparison),
            TokenType.GreaterEqual => Operators.GreaterThanOrEqual(left, right, ctx.Context.Config.StringComparison),
            TokenType.Amp => Operators.BitwiseAnd(left, right),
            TokenType.Pipe => Operators.BitwiseOr(left, right),
            TokenType.Caret => Operators.BitwiseXor(left, right),
            TokenType.LessLess => Operators.LeftShift(left, right),
            TokenType.GreaterGreater => Operators.RightShift(left, right),
            TokenType.GreaterGreaterGreater => Operators.UnsignedRightShift(left, right),
            TokenType.StarStar => Operators.Power(left, right),
            TokenType.In => Operators.InOperator(left, right),
            TokenType.Like => Operators.Like(left, right, ctx.Context.Config.StringComparison),
            TokenType.EqualTilde => Operators.RegexMatch(left, right),
            TokenType.BangTilde => Operators.RegexNotMatch(left, right),
            TokenType.LessEqualGreater => Operators.Spaceship(left, right),
            _ => throw new BindingNotSupportedException(
                $"Bound binary operator '{binary.Operator}' is not implemented")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? EvaluateInt32Fast(TokenType op, int left, int right, bool isChecked) => op switch
    {
        TokenType.Plus => isChecked ? checked(left + right) : left + right,
        TokenType.Minus => isChecked ? checked(left - right) : left - right,
        TokenType.Star => isChecked ? checked(left * right) : left * right,
        TokenType.Slash => left / right,
        TokenType.Percent => left % right,
        TokenType.EqualEqual => left == right ? BoxedConstants.True : BoxedConstants.False,
        TokenType.BangEqual => left != right ? BoxedConstants.True : BoxedConstants.False,
        TokenType.Less => left < right ? BoxedConstants.True : BoxedConstants.False,
        TokenType.LessEqual => left <= right ? BoxedConstants.True : BoxedConstants.False,
        TokenType.Greater => left > right ? BoxedConstants.True : BoxedConstants.False,
        TokenType.GreaterEqual => left >= right ? BoxedConstants.True : BoxedConstants.False,
        TokenType.Amp => left & right,
        TokenType.Pipe => left | right,
        TokenType.Caret => left ^ right,
        TokenType.LessLess => left << right,
        TokenType.GreaterGreater => left >> right,
        _ => null
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? EvaluateDoubleFast(TokenType op, double left, double right) => op switch
    {
        TokenType.Plus => left + right,
        TokenType.Minus => left - right,
        TokenType.Star => left * right,
        TokenType.Slash => left / right,
        TokenType.Percent => left % right,
        TokenType.EqualEqual => left == right ? BoxedConstants.True : BoxedConstants.False,
        TokenType.BangEqual => left != right ? BoxedConstants.True : BoxedConstants.False,
        TokenType.Less => left < right ? BoxedConstants.True : BoxedConstants.False,
        TokenType.LessEqual => left <= right ? BoxedConstants.True : BoxedConstants.False,
        TokenType.Greater => left > right ? BoxedConstants.True : BoxedConstants.False,
        TokenType.GreaterEqual => left >= right ? BoxedConstants.True : BoxedConstants.False,
        _ => null
    };

    private static bool IsNaN(object value) => value is double and Double.NaN || value is float and Single.NaN;

    public static async ValueTask<object?> EvaluateAsync(BoundBinaryExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        if (node.Left is not BoundBinaryExpr)
            return EvaluateBinarySingle(node, await ctx.EvaluateAsync(node.Left, ct), ctx, ct);

        var chain = new List<BoundBinaryExpr>();
        BoundExpr leftmost = node;
        while (leftmost is BoundBinaryExpr b)
        {
            chain.Add(b);
            leftmost = b.Left;
        }

        for (var i = chain.Count - 1; i > 0; i--)
            ctx.Tracer?.Push(chain[i]);

        var result = await ctx.EvaluateAsync(leftmost, ct);
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            result = await EvaluateBinarySingleAsync(chain[i], result, ctx, ct);
            if (i > 0)
                ctx.Tracer?.Pop(result);
        }
        return result;
    }

    private static async ValueTask<object?> EvaluateBinarySingleAsync(BoundBinaryExpr binary, object? left, EvaluationContext ctx, CancellationToken ct)
    {
        var right = await ctx.EvaluateAsync(binary.Right, ct);

        if (binary.PromotedType is { } promoted && left != null && right != null
            && left.GetType() == binary.Left.StaticType.ClrType
            && right.GetType() == binary.Right.StaticType.ClrType)
        {
            if (promoted == typeof(int) && left is int li && right is int ri)
                return EvaluateInt32Fast(binary.Operator, li, ri, ctx.IsChecked);

            if (promoted == typeof(double) && left is double ld && right is double rd)
                return EvaluateDoubleFast(binary.Operator, ld, rd);

            if ((IsNaN(left) || IsNaN(right)) &&
                binary.Operator is TokenType.EqualEqual or TokenType.BangEqual or
                TokenType.Less or TokenType.LessEqual or TokenType.Greater or TokenType.GreaterEqual)
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
                _ => EvaluateBinaryFallback(binary, left, right, ctx, ct)
            };
        }

        (left, right) = NumericPromotionRuntime.ApplyConstantNumericPromotion(
            left,
            binary.Left.Kind == BoundNodeKind.Literal,
            right,
            binary.Right.Kind == BoundNodeKind.Literal);

        return EvaluateBinaryFallback(binary, left, right, ctx, ct);
    }
}
