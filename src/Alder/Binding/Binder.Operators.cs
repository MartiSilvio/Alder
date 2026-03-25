using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding;

internal sealed partial class Binder
{
    private BoundCastExpr BindCast(CastExpr cast, BindingContext context)
    {
        var expression = Bind(cast.Expression, context);
        var targetType = context.RuntimeContext.TypeResolver.ResolveType(cast.TargetType.Lexeme);
        var sourceStaticType = cast.Expression is IdentifierExpr && expression.StaticType is not BoundUnknownType
            ? expression.StaticType.ClrType
            : (Type?)null;
        return new BoundCastExpr(expression, targetType, sourceStaticType, new BoundType(targetType));
    }

    private BoundIsPatternExpr BindIsPattern(IsPatternExpr isPattern, BindingContext context)
    {
        var expression = Bind(isPattern.Expression, context);
        return new BoundIsPatternExpr(expression, isPattern.Pattern, new BoundType(typeof(bool)));
    }

    private BoundAsExpr BindAs(AsExpr asExpr, BindingContext context)
    {
        var expression = Bind(asExpr.Expression, context);
        var targetType = context.RuntimeContext.TypeResolver.ResolveType(asExpr.TargetType.Lexeme);
        return new BoundAsExpr(expression, targetType, new BoundType(targetType));
    }

    private BoundExpr BindBinary(BinaryExpr binary, BindingContext context)
    {
        var chain = new List<BinaryExpr>();
        Expr leftmost = binary;
        while (leftmost is BinaryExpr b)
        {
            chain.Add(b);
            leftmost = b.Left;
        }

        var result = Bind(leftmost, context);
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var link = chain[i];
            var right = Bind(link.Right, context);
            if (result.HasErrors || right.HasErrors)
            {
                result = new BoundBinaryExpr(link.Op.Type, result, right, BoundType.Unknown) { Span = link.Span, HasErrors = true };
                continue;
            }
            var resultClrType = InferBinaryResultType(link.Op.Type, result.StaticType, right.StaticType);
            var resultType = resultClrType == typeof(object) ? BoundType.Unknown : new BoundType(resultClrType);
            result = new BoundBinaryExpr(link.Op.Type, result, right, resultType)
            {
                Span = link.Span,
                PromotedType = ComputeBinaryPromotedType(link.Op.Type, result, right)
            };
        }

        return result;
    }

    private BoundExpr BindUnary(UnaryExpr unary, BindingContext context)
    {
        var operand = Bind(unary.Right, context);
        if (operand.HasErrors)
            return new BoundUnaryExpr(unary.Op.Type, operand, BoundType.Unknown) { HasErrors = true };

        // ECMA-334 §6.4.5.3: -2147483648 (literal) is int; -9223372036854775808L is long
        if (unary.Op.Type == TokenType.Minus && operand is BoundLiteralExpr literal)
        {
            if (literal.Value is uint u && u == (uint)int.MaxValue + 1)
                return BoundLiteralExpr.FromValue(int.MinValue);
            if (literal.Value is ulong ul && ul == (ulong)long.MaxValue + 1)
                return BoundLiteralExpr.FromValue(long.MinValue);
        }

        var resultType = unary.Op.Type == TokenType.Bang
            ? typeof(bool)
            : InferUnaryResultType(operand.StaticType.ClrType, unary.Op.Type);
        return new BoundUnaryExpr(unary.Op.Type, operand, new BoundType(resultType))
        {
            PromotedType = ComputeUnaryPromotedType(unary.Op.Type, operand.StaticType.ClrType)
        };
    }

    private static Type? ComputeUnaryPromotedType(TokenType op, Type operandType)
    {
        if (operandType == typeof(object) || operandType.IsEnum)
            return null;

        return op switch
        {
            TokenType.Bang when operandType == typeof(bool) => typeof(bool),
            TokenType.Tilde when IsIntegralOrChar(operandType) => NormalizeArithmeticType(operandType),
            TokenType.Minus when TypeHelpers.IsArithmetic(operandType) =>
                operandType == typeof(uint) ? typeof(long) : NormalizeArithmeticType(operandType),
            TokenType.Plus when TypeHelpers.IsArithmetic(operandType) => NormalizeArithmeticType(operandType),
            _ => null
        };
    }

    private static bool IsIntegralOrChar(Type type) =>
        Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.UInt64 or TypeCode.Char;

    private BoundExpr BindLogical(LogicalExpr logical, BindingContext context)
    {
        var chain = new List<LogicalExpr>();
        Expr leftmost = logical;
        while (leftmost is LogicalExpr l)
        {
            chain.Add(l);
            leftmost = l.Left;
        }

        var result = Bind(leftmost, context);
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var link = chain[i];
            var right = Bind(link.Right, context);
            if (result.HasErrors || right.HasErrors)
            {
                result = new BoundLogicalExpr(link.Op.Type, result, right, new BoundType(typeof(bool))) { Span = link.Span, HasErrors = true };
                continue;
            }
            result = new BoundLogicalExpr(link.Op.Type, result, right, new BoundType(typeof(bool))) { Span = link.Span };
        }

        return result;
    }

    private BoundExpr BindNullCoalesce(NullCoalesceExpr nullCoalesce, BindingContext context)
    {
        var chain = new List<NullCoalesceExpr>();
        Expr leftmost = nullCoalesce;
        while (leftmost is NullCoalesceExpr nc)
        {
            chain.Add(nc);
            leftmost = nc.Left;
        }

        var result = Bind(leftmost, context);
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var link = chain[i];
            var right = Bind(link.Right, context);
            if (result.HasErrors || right.HasErrors)
            {
                result = new BoundNullCoalesceExpr(result, right, BoundType.Unknown) { Span = link.Span, HasErrors = true };
                continue;
            }
            result = new BoundNullCoalesceExpr(result, right, new BoundType(GetCommonType(result.StaticType.ClrType, right.StaticType.ClrType))) { Span = link.Span };
        }

        return result;
    }

    private BoundExpr BindConditional(ConditionalExpr conditional, BindingContext context)
    {
        var condition = Bind(conditional.Condition, context);
        var thenBranch = Bind(conditional.ThenBranch, context);
        var elseBranch = Bind(conditional.ElseBranch, context);
        if (condition.HasErrors || thenBranch.HasErrors || elseBranch.HasErrors)
            return new BoundConditionalExpr(condition, thenBranch, elseBranch, BoundType.Unknown) { HasErrors = true };
        return new BoundConditionalExpr(
            condition,
            thenBranch,
            elseBranch,
            new BoundType(GetCommonType(thenBranch.StaticType.ClrType, elseBranch.StaticType.ClrType)));
    }

    private BoundCheckedExpr BindCheckedExpr(CheckedExpr checkedExpr, BindingContext context)
    {
        var expression = Bind(checkedExpr.Expression, context);
        return new BoundCheckedExpr(expression, checkedExpr.IsChecked, expression.StaticType);
    }

    private BoundChainedComparisonExpr BindChainedComparison(ChainedComparisonExpr chainedComparison, BindingContext context)
    {
        var operands = chainedComparison.Operands
            .Select(operand => Bind(operand, context))
            .ToImmutableArray();
        var operators = chainedComparison.Operators
            .Select(@operator => @operator.Type)
            .ToImmutableArray();
        return new BoundChainedComparisonExpr(operands, operators, new BoundType(typeof(bool)));
    }

    private static Type InferBinaryResultType(TokenType op, BoundType left, BoundType right)
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

        // When one operand is genuinely typed as object (not BoundUnknownType), we can
        // infer the result from the known arithmetic operand. This is needed for lambda
        // return type inference during overload resolution, where parameters are typed
        // as object but the other operand has a concrete type.
        // When unknown, the runtime type could be anything — inferring a specific type
        // would mislead downstream PromotedType into truncating the actual runtime value.
        if (left is not BoundUnknownType && leftType == typeof(object) && TypeHelpers.IsArithmetic(rightType))
            return InferArithmeticResultType(rightType, rightType, op);
        if (right is not BoundUnknownType && rightType == typeof(object) && TypeHelpers.IsArithmetic(leftType))
            return InferArithmeticResultType(leftType, leftType, op);

        return typeof(object);
    }

    private static Type InferArithmeticResultType(Type leftType, Type rightType, TokenType op)
    {
        // ECMA-style numeric promotion for arithmetic result typing.
        // Use object only for ambiguous/invalid static mixes where runtime dispatch must decide.
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
    private static Type InferUnaryResultType(Type operandType, TokenType op)
    {
        var promoted = NormalizeArithmeticType(operandType);
        if (op == TokenType.Minus && promoted == typeof(uint))
            return typeof(long);
        return promoted;
    }

    private static Type NormalizeArithmeticType(Type type)
    {
        if (type == typeof(char) || type == typeof(byte) || type == typeof(sbyte) ||
            type == typeof(short) || type == typeof(ushort))
            return typeof(int);
        return type;
    }

    private static bool IsSignedIntegralType(Type type)
    {
        return type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(int) ||
               type == typeof(long);
    }

    private static Type GetCommonType(Type leftType, Type rightType)
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
            if (!IsIntegralOrChar(leftType) || !IsIntegralOrChar(rightType))
                return null;
            return NormalizeArithmeticType(leftType);
        }

        if (op is TokenType.Amp or TokenType.Pipe or TokenType.Caret)
        {
            if (!IsIntegralOrChar(leftType) || !IsIntegralOrChar(rightType))
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

    // ECMA-334 §10.2.11: int constant → uint/ulong if in range; long constant → ulong if non-negative.
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
