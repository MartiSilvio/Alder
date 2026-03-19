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

    private BoundBinaryExpr BindBinary(BinaryExpr binary, BindingContext context)
    {
        var left = Bind(binary.Left, context);
        var right = Bind(binary.Right, context);
        var resultType = InferBinaryResultType(binary.Op.Type, left.StaticType, right.StaticType);

        return new BoundBinaryExpr(binary.Op.Type, left, right, resultType);
    }

    private BoundUnaryExpr BindUnary(UnaryExpr unary, BindingContext context)
    {
        var operand = Bind(unary.Right, context);
        var resultType = unary.Op.Type == TokenType.Bang ? typeof(bool) : operand.StaticType;
        return new BoundUnaryExpr(unary.Op.Type, operand, resultType);
    }

    private BoundLogicalExpr BindLogical(LogicalExpr logical, BindingContext context)
    {
        var left = Bind(logical.Left, context);
        var right = Bind(logical.Right, context);
        return new BoundLogicalExpr(logical.Op.Type, left, right, typeof(bool));
    }

    private BoundNullCoalesceExpr BindNullCoalesce(NullCoalesceExpr nullCoalesce, BindingContext context)
    {
        var left = Bind(nullCoalesce.Left, context);
        var right = Bind(nullCoalesce.Right, context);
        return new BoundNullCoalesceExpr(
            left,
            right,
            GetCommonType(left.StaticType, right.StaticType));
    }

    private BoundConditionalExpr BindConditional(ConditionalExpr conditional, BindingContext context)
    {
        var condition = Bind(conditional.Condition, context);
        var thenBranch = Bind(conditional.ThenBranch, context);
        var elseBranch = Bind(conditional.ElseBranch, context);
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

    private static Type NormalizeArithmeticType(Type type)
    {
        if (type == typeof(char))
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
