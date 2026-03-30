using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(BinaryExpr))]
internal static class BinaryBinder
{
    public static BoundExpr Bind(BinaryExpr expr, BindingContext context, BinderContext binder)
    {
        if (expr.Left is not BinaryExpr)
            return BindSingle(expr, binder.Bind(expr.Left, context), binder.Bind(expr.Right, context));

        var chain = new List<BinaryExpr>();
        Expr leftmost = expr;
        while (leftmost is BinaryExpr b)
        {
            chain.Add(b);
            leftmost = b.Left;
        }

        var result = binder.Bind(leftmost, context);
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var link = chain[i];
            result = BindSingle(link, result, binder.Bind(link.Right, context));
        }

        return result;
    }

    private static BoundExpr BindSingle(BinaryExpr link, BoundExpr left, BoundExpr right)
    {
        if (left.HasErrors || right.HasErrors)
            return new BoundBinaryExpr(link.Op.Type, left, right, BoundType.Unknown) { Span = link.Span, HasErrors = true };

        var resultClrType = InferBinaryResultType(link.Op.Type, left.StaticType, right.StaticType);
        var resultType = resultClrType == typeof(object) ? BoundType.Unknown : new BoundType(resultClrType);
        return new BoundBinaryExpr(link.Op.Type, left, right, resultType)
        {
            Span = link.Span,
            PromotedType = ComputeBinaryPromotedType(link.Op.Type, left, right)
        };
    }

    internal static Type InferBinaryResultType(TokenType op, BoundType left, BoundType right)
    {
        var leftType = left.ClrType;
        var rightType = right.ClrType;

        if (op is TokenType.EqualEqual or TokenType.BangEqual or TokenType.EqualEqualEqual or TokenType.BangEqualEqual or
            TokenType.Less or TokenType.LessEqual or TokenType.Greater or TokenType.GreaterEqual or
            TokenType.In or TokenType.Like or TokenType.EqualTilde or TokenType.BangTilde)
        {
            return typeof(bool);
        }

        if (op == TokenType.LessEqualGreater)
            return typeof(int);

        if (op == TokenType.Plus && (leftType == typeof(string) || rightType == typeof(string)))
            return typeof(string);

        if (op == TokenType.StarStar)
            return typeof(double);

        if (TypeHelpers.IsArithmetic(leftType) && TypeHelpers.IsArithmetic(rightType))
            return InferArithmeticResultType(leftType, rightType, op);

        if (left is not BoundUnknownType && leftType == typeof(object) && TypeHelpers.IsArithmetic(rightType))
            return InferArithmeticResultType(rightType, rightType, op);
        if (right is not BoundUnknownType && rightType == typeof(object) && TypeHelpers.IsArithmetic(leftType))
            return InferArithmeticResultType(leftType, leftType, op);

        return typeof(object);
    }

    internal static Type InferArithmeticResultType(Type leftType, Type rightType, TokenType op)
    {
        var normalizedLeft = NormalizeArithmeticType(leftType);
        var normalizedRight = NormalizeArithmeticType(rightType);

        if (op is TokenType.LessLess or TokenType.GreaterGreater or TokenType.GreaterGreaterGreater)
            return normalizedLeft;

        if (normalizedLeft == normalizedRight)
            return normalizedLeft;

        if (normalizedLeft == typeof(decimal) || normalizedRight == typeof(decimal))
            return typeof(decimal);

        if (normalizedLeft == typeof(double) || normalizedRight == typeof(double))
            return typeof(double);

        if (normalizedLeft == typeof(float) || normalizedRight == typeof(float))
            return typeof(float);

        if (normalizedLeft == typeof(ulong) || normalizedRight == typeof(ulong))
        {
            var other = normalizedLeft == typeof(ulong) ? normalizedRight : normalizedLeft;
            if (IsSignedIntegralType(other))
                return typeof(object);
            return typeof(ulong);
        }

        if (normalizedLeft == typeof(long) || normalizedRight == typeof(long))
            return typeof(long);

        if (normalizedLeft == typeof(uint) || normalizedRight == typeof(uint))
        {
            var other = normalizedLeft == typeof(uint) ? normalizedRight : normalizedLeft;
            if (IsSignedIntegralType(other))
                return typeof(long);
            return typeof(uint);
        }

        return typeof(int);
    }

    /// <summary>
    /// ECMA-334 §12.4.7.2: sbyte/byte/short/ushort/char → int, unary - on uint → long.
    /// </summary>
    internal static Type NormalizeArithmeticType(Type type)
    {
        if (type == typeof(char) || type == typeof(byte) || type == typeof(sbyte) ||
            type == typeof(short) || type == typeof(ushort))
            return typeof(int);
        return type;
    }

    internal static bool IsSignedIntegralType(Type type)
    {
        return type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(int) ||
               type == typeof(long);
    }

    internal static Type GetCommonType(Type leftType, Type rightType)
    {
        if (leftType == rightType)
            return leftType;

        if (TypeHelpers.IsArithmetic(leftType) && TypeHelpers.IsArithmetic(rightType))
            return NumericDispatch.GetResultType(leftType, rightType);

        if (leftType.IsAssignableFrom(rightType))
            return leftType;
        if (rightType.IsAssignableFrom(leftType))
            return rightType;

        if (leftType == typeof(string) || rightType == typeof(string))
            return typeof(string);

        return typeof(object);
    }

    private static Type? ComputeBinaryPromotedType(TokenType op, BoundExpr left, BoundExpr right)
    {
        if (!IsFastPathOperator(op))
            return null;

        var leftType = left.StaticType.ClrType;
        var rightType = right.StaticType.ClrType;
        if (!TypeHelpers.IsArithmetic(leftType) || !TypeHelpers.IsArithmetic(rightType))
            return null;
        if (leftType.IsEnum || rightType.IsEnum)
            return null;

        if (op is TokenType.LessLess or TokenType.GreaterGreater)
        {
            if (!UnaryBinder.IsIntegralOrChar(leftType) || !UnaryBinder.IsIntegralOrChar(rightType))
                return null;
            return NormalizeArithmeticType(leftType);
        }

        if (op is TokenType.Amp or TokenType.Pipe or TokenType.Caret)
        {
            if (!UnaryBinder.IsIntegralOrChar(leftType) || !UnaryBinder.IsIntegralOrChar(rightType))
                return null;
        }

        if ((leftType == typeof(decimal) && (rightType == typeof(float) || rightType == typeof(double))) ||
            (rightType == typeof(decimal) && (leftType == typeof(float) || leftType == typeof(double))))
            return null;

        if (TryConstantPromotion(leftType, rightType, left as BoundLiteralExpr, right as BoundLiteralExpr, out var promoted))
            return promoted;

        var result = NumericDispatch.GetResultType(leftType, rightType);
        return TypeHelpers.IsArithmetic(result) ? result : null;
    }

    private static bool TryConstantPromotion(
        Type leftType, Type rightType,
        BoundLiteralExpr? leftLiteral, BoundLiteralExpr? rightLiteral,
        out Type promoted)
    {
        promoted = null!;
        if (leftType == typeof(uint) && IsNonNegativeIntConstant(rightLiteral) ||
            rightType == typeof(uint) && IsNonNegativeIntConstant(leftLiteral))
        {
            promoted = typeof(uint);
            return true;
        }

        if (leftType == typeof(ulong) && IsNonNegativeIntOrLongConstant(rightLiteral) ||
            rightType == typeof(ulong) && IsNonNegativeIntOrLongConstant(leftLiteral))
        {
            promoted = typeof(ulong);
            return true;
        }

        return false;
    }

    private static bool IsNonNegativeIntConstant(BoundLiteralExpr? literal) =>
        literal?.Value is int and >= 0;

    private static bool IsNonNegativeIntOrLongConstant(BoundLiteralExpr? literal) =>
        literal?.Value is int and >= 0 or long and >= 0;

    private static bool IsFastPathOperator(TokenType op) =>
        op is TokenType.Plus or TokenType.Minus or TokenType.Star or TokenType.Slash or TokenType.Percent or
            TokenType.EqualEqual or TokenType.BangEqual or
            TokenType.Less or TokenType.LessEqual or TokenType.Greater or TokenType.GreaterEqual or
            TokenType.Amp or TokenType.Pipe or TokenType.Caret or
            TokenType.LessLess or TokenType.GreaterGreater;
}
