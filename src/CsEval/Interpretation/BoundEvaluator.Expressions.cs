using CsEval.Binding;
using CsEval.Binding.BoundNodes;
using CsEval.Diagnostics;
using CsEval.Parsing;
using CsEval.Runtime;
using CsEval.Runtime.Extensions;
using CsEval.Runtime.Semantics;
using System.Text;

namespace CsEval.Interpretation;

internal sealed partial class BoundEvaluator
{
    private object? EvaluateCast(BoundCastExpr cast)
    {
        var value = Evaluate(cast.Expression);
        return TypeHelpers.ExplicitCast(value, cast.TargetType, cast.SourceStaticType, _isChecked);
    }

    private object? EvaluateAs(BoundAsExpr asExpr)
    {
        var value = Evaluate(asExpr.Expression);
        return TypeHelpers.TryAs(value, asExpr.TargetType);
    }

    private object? EvaluateIsPattern(BoundIsPatternExpr isPattern)
    {
        var value = Evaluate(isPattern.Expression);
        return MatchPattern(value, isPattern.Pattern);
    }

    private object? EvaluateInterpolatedString(BoundInterpolatedStringExpr interpolatedString)
    {
        var sb = new StringBuilder();
        foreach (var part in interpolatedString.Parts)
        {
            switch (part)
            {
                case BoundInterpolatedTextPart text:
                    sb.Append(text.Text);
                    break;
                case BoundInterpolatedExpressionPart expressionPart:
                {
                    var value = Evaluate(expressionPart.Expression);
                    if (expressionPart.AlignmentSpecifier != null || expressionPart.FormatSpecifier != null)
                    {
                        var format = "{0";
                        if (expressionPart.AlignmentSpecifier != null)
                            format += "," + expressionPart.AlignmentSpecifier;
                        if (expressionPart.FormatSpecifier != null)
                            format += ":" + expressionPart.FormatSpecifier;
                        format += "}";
                        sb.Append(string.Format(format, value));
                    }
                    else
                    {
                        sb.Append(value?.ToString() ?? string.Empty);
                    }

                    break;
                }
                default:
                    throw new BindingNotSupportedException(
                        $"Bound interpolated part '{part.GetType().Name}' is not implemented");
            }
        }

        return sb.ToString();
    }

    private object? EvaluateUnary(BoundUnaryExpr unary)
    {
        var operand = Evaluate(unary.Operand);
        return unary.Operator switch
        {
            TokenType.Minus => Operators.Negate(operand, _isChecked),
            TokenType.Plus => Operators.UnaryPlus(operand),
            TokenType.Bang => Operators.LogicalNot(operand),
            TokenType.Tilde => Operators.BitwiseNot(operand),
            _ => throw new BindingNotSupportedException(
                $"Bound unary operator '{unary.Operator}' is not implemented")
        };
    }

    private object? EvaluateBinary(BoundBinaryExpr binary)
    {
        var chain = new List<BoundBinaryExpr>();
        BoundExpr leftmost = binary;
        while (leftmost is BoundBinaryExpr b)
        {
            chain.Add(b);
            leftmost = b.Left;
        }

        var result = Evaluate(leftmost);
        for (var i = chain.Count - 1; i >= 0; i--)
            result = EvaluateBinarySingle(chain[i], result);
        return result;
    }

    private object? EvaluateBinarySingle(BoundBinaryExpr binary, object? left)
    {
        var right = Evaluate(binary.Right);

        (left, right) = NumericPromotionRuntime.ApplyConstantNumericPromotion(
            left,
            binary.Left.Kind == BoundNodeKind.Literal,
            right,
            binary.Right.Kind == BoundNodeKind.Literal);

        return binary.Operator switch
        {
            TokenType.Plus => Operators.Add(left, right, _options, _context, _isChecked,
                isStringContext: binary.Left.StaticType == typeof(string) || binary.Right.StaticType == typeof(string)),
            TokenType.Minus => Operators.Subtract(left, right, _isChecked),
            TokenType.Star => Operators.Multiply(left, right, _options, _isChecked),
            TokenType.Slash => Operators.Divide(left, right),
            TokenType.Percent => Operators.Modulo(left, right),
            TokenType.EqualEqual => Operators.Equals(left, right),
            TokenType.BangEqual => Operators.NotEquals(left, right),
            TokenType.EqualEqualEqual => Operators.StrictEquals(left, right),
            TokenType.BangEqualEqual => Operators.StrictNotEquals(left, right),
            TokenType.Less => Operators.LessThan(left, right, _options),
            TokenType.LessEqual => Operators.LessThanOrEqual(left, right, _options),
            TokenType.Greater => Operators.GreaterThan(left, right, _options),
            TokenType.GreaterEqual => Operators.GreaterThanOrEqual(left, right, _options),
            TokenType.Amp => Operators.BitwiseAnd(left, right),
            TokenType.Pipe => Operators.BitwiseOr(left, right),
            TokenType.Caret => Operators.BitwiseXor(left, right),
            TokenType.LessLess => Operators.LeftShift(left, right),
            TokenType.GreaterGreater => Operators.RightShift(left, right),
            TokenType.GreaterGreaterGreater => Operators.UnsignedRightShift(left, right),
            TokenType.StarStar => Operators.Power(left, right),
            TokenType.In => Operators.InOperator(left, right),
            TokenType.Like => Operators.Like(left, right, _options),
            TokenType.EqualTilde => Operators.RegexMatch(left, right),
            TokenType.BangTilde => Operators.RegexNotMatch(left, right),
            TokenType.LessEqualGreater => Operators.Spaceship(left, right),
            _ => throw new BindingNotSupportedException(
                $"Bound binary operator '{binary.Operator}' is not implemented")
        };
    }

    private object? EvaluateLogical(BoundLogicalExpr logical)
    {
        var left = Evaluate(logical.Left);
        var opLexeme = TokenLexemes.GetCanonical(logical.Operator);

        // §12.14.2 + §12.13.5: nullable bool conditional operators
        if (logical.Left.StaticType == typeof(bool?) || logical.Right.StaticType == typeof(bool?))
        {
            return EvaluateNullableBoolLogical(left as bool?, logical);
        }

        if (left is not bool leftBool)
        {
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                opLexeme,
                TypeNameFormatter.Of(left),
                GetLogicalExpressionTypeName(logical.Right));
        }

        if (logical.Operator == TokenType.PipePipe)
        {
            if (leftBool)
                return true;
        }
        else if (logical.Operator == TokenType.AmpAmp)
        {
            if (!leftBool)
                return false;
        }
        else
        {
            throw new BindingNotSupportedException(
                $"Bound logical operator '{logical.Operator}' is not implemented");
        }

        var right = Evaluate(logical.Right);
        if (right is not bool)
        {
            throw new CsEvalException(
                DiagnosticDescriptors.BadBinaryOps,
                opLexeme,
                left.GetType().Name,
                TypeNameFormatter.Of(right));
        }

        return (bool)right;
    }

    // §12.13.5 + §12.14.2: Three-value logic for bool? && and bool? ||
    private object? EvaluateNullableBoolLogical(bool? left, BoundLogicalExpr logical)
    {
        if (logical.Operator == TokenType.AmpAmp)
        {
            if (left == false) return false;
            var right = Evaluate(logical.Right) as bool?;
            if (left == true) return right;
            return right == false ? false : null; // null && true = null, null && null = null
        }

        if (logical.Operator == TokenType.PipePipe)
        {
            if (left == true) return true;
            var right = Evaluate(logical.Right) as bool?;
            if (left == false) return right;
            return right == true ? true : null; // null || false = null, null || null = null
        }

        throw new BindingNotSupportedException(
            $"Bound logical operator '{logical.Operator}' is not implemented");
    }

    private object? EvaluateNullCoalesce(BoundNullCoalesceExpr nullCoalesce)
    {
        var left = Evaluate(nullCoalesce.Left);
        return left ?? Evaluate(nullCoalesce.Right);
    }

    private object? EvaluateConditional(BoundConditionalExpr conditional)
    {
        var condition = Evaluate(conditional.Condition);
        var result = TypeHelpers.RequireBoolean(condition)
            ? Evaluate(conditional.ThenBranch)
            : Evaluate(conditional.ElseBranch);

        var thenType = conditional.ThenBranch.StaticType;
        var elseType = conditional.ElseBranch.StaticType;

        if (result != null &&
            thenType != typeof(object) &&
            elseType != typeof(object) &&
            TypeHelpers.IsArithmetic(thenType) &&
            TypeHelpers.IsArithmetic(elseType) &&
            thenType != elseType)
        {
            var resultType = NumericDispatch.GetResultType(thenType, elseType);
            return NumericDispatch.PromoteToType(result, resultType);
        }

        return result;
    }

    private object? EvaluateChecked(BoundCheckedExpr checkedExpr)
    {
        var previous = _isChecked;
        _isChecked = checkedExpr.IsChecked;
        try
        {
            return Evaluate(checkedExpr.Expression);
        }
        finally
        {
            _isChecked = previous;
        }
    }

    private object? EvaluateChainedComparison(BoundChainedComparisonExpr chainedComparison)
    {
        var previousValue = Evaluate(chainedComparison.Operands[0]);

        for (var i = 0; i < chainedComparison.Operators.Length; i++)
        {
            var nextValue = Evaluate(chainedComparison.Operands[i + 1]);
            if (!ChainedComparisonHelper.PerformComparison(
                    previousValue,
                    nextValue,
                    chainedComparison.Operators[i],
                    _options))
            {
                return false;
            }

            previousValue = nextValue;
        }

        return true;
    }

    private static string GetLogicalExpressionTypeName(BoundExpr expr)
    {
        if (expr is BoundLiteralExpr { Value: null })
            return TypeNameFormatter.Null;

        if (expr is BoundLiteralExpr { Value: { } value })
            return value.GetType().Name;

        return expr.StaticType == typeof(object)
            ? "unknown"
            : expr.StaticType.Name;
    }
}
