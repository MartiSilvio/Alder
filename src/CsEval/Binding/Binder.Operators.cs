using CsEval.Binding.BoundNodes;
using CsEval.Parsing;
using CsEval.Runtime;
using System.Collections.Immutable;

namespace CsEval.Binding;

internal sealed partial class Binder
{
    private BoundCastExpr BindCast(CastExpr cast, BindingContext context)
    {
        var expression = Bind(cast.Expression, context);
        var targetType = context.RuntimeContext.TypeResolver.ResolveType(cast.TargetType.Lexeme);
        var sourceStaticType = cast.Expression is IdentifierExpr ? expression.StaticType : null;
        return new BoundCastExpr(expression, targetType, sourceStaticType, targetType);
    }

    private BoundIsPatternExpr BindIsPattern(IsPatternExpr isPattern, BindingContext context)
    {
        var expression = Bind(isPattern.Expression, context);
        return new BoundIsPatternExpr(expression, isPattern.Pattern, typeof(bool));
    }

    private BoundAsExpr BindAs(AsExpr asExpr, BindingContext context)
    {
        var expression = Bind(asExpr.Expression, context);
        var targetType = context.RuntimeContext.TypeResolver.ResolveType(asExpr.TargetType.Lexeme);
        return new BoundAsExpr(expression, targetType, targetType);
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
                result = new BoundBinaryExpr(link.Op.Type, result, right, typeof(object)) { Span = link.Span, HasErrors = true };
                continue;
            }
            var resultType = InferBinaryResultType(link.Op.Type, result.StaticType, right.StaticType);
            result = new BoundBinaryExpr(link.Op.Type, result, right, resultType) { Span = link.Span };
        }

        return result;
    }

    private BoundExpr BindUnary(UnaryExpr unary, BindingContext context)
    {
        var operand = Bind(unary.Right, context);
        if (operand.HasErrors)
            return new BoundUnaryExpr(unary.Op.Type, operand, typeof(object)) { HasErrors = true };

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
            : InferUnaryResultType(operand.StaticType, unary.Op.Type);
        return new BoundUnaryExpr(unary.Op.Type, operand, resultType);
    }

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
                result = new BoundLogicalExpr(link.Op.Type, result, right, typeof(bool)) { Span = link.Span, HasErrors = true };
                continue;
            }
            result = new BoundLogicalExpr(link.Op.Type, result, right, typeof(bool)) { Span = link.Span };
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
                result = new BoundNullCoalesceExpr(result, right, typeof(object)) { Span = link.Span, HasErrors = true };
                continue;
            }
            result = new BoundNullCoalesceExpr(result, right, GetCommonType(result.StaticType, right.StaticType)) { Span = link.Span };
        }

        return result;
    }

    private BoundExpr BindConditional(ConditionalExpr conditional, BindingContext context)
    {
        var condition = Bind(conditional.Condition, context);
        var thenBranch = Bind(conditional.ThenBranch, context);
        var elseBranch = Bind(conditional.ElseBranch, context);
        if (condition.HasErrors || thenBranch.HasErrors || elseBranch.HasErrors)
            return new BoundConditionalExpr(condition, thenBranch, elseBranch, typeof(object)) { HasErrors = true };
        return new BoundConditionalExpr(
            condition,
            thenBranch,
            elseBranch,
            GetCommonType(thenBranch.StaticType, elseBranch.StaticType));
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
        return new BoundChainedComparisonExpr(operands, operators, typeof(bool));
    }

    private static Type InferBinaryResultType(TokenType op, Type leftType, Type rightType)
    {
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

        // Extended-mode ** delegates to Math.Pow which always returns double
        if (op == TokenType.StarStar)
            return typeof(double);

        if (TypeHelpers.IsArithmetic(leftType) && TypeHelpers.IsArithmetic(rightType))
            return InferArithmeticResultType(leftType, rightType, op);

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
}
