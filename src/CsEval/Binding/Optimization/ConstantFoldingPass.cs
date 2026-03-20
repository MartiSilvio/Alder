using CsEval.Binding.BoundNodes;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Binding.Optimization;

internal sealed class ConstantFoldingPass : BoundExprRewriter
{
    protected override BoundExpr VisitUnary(BoundUnaryExpr node)
    {
        var rewritten = (BoundUnaryExpr)base.VisitUnary(node);
        if (rewritten.Operand is not BoundLiteralExpr { Value: not null } operand)
            return rewritten;

        try
        {
            var result = rewritten.Operator switch
            {
                TokenType.Minus => Operators.Negate(operand.Value),
                TokenType.Plus => Operators.UnaryPlus(operand.Value),
                TokenType.Bang => Operators.LogicalNot(operand.Value),
                TokenType.Tilde => Operators.BitwiseNot(operand.Value),
                _ => null
            };

            if (result == null) return rewritten;
            var folded = BoundLiteralExpr.FromValue(result);
            folded.Span = rewritten.Span;
            return folded;
        }
        catch
        {
            return rewritten;
        }
    }

    protected override BoundExpr RevisitBinary(BoundExpr node)
    {
        if (node is not BoundBinaryExpr binary) return node;
        return TryFoldBinary(binary);
    }

    private static BoundExpr TryFoldBinary(BoundBinaryExpr binary)
    {
        if (binary.Left is not BoundLiteralExpr { Value: not null } left ||
            binary.Right is not BoundLiteralExpr { Value: not null } right)
            return binary;

        try
        {
            var result = binary.Operator switch
            {
                TokenType.Plus => FoldAdd(left.Value, right.Value),
                TokenType.Minus => NumericDispatch.Subtract(left.Value, right.Value),
                TokenType.Star => NumericDispatch.Multiply(left.Value, right.Value),
                TokenType.Slash => NumericDispatch.Divide(left.Value, right.Value),
                TokenType.Percent => NumericDispatch.Modulo(left.Value, right.Value),
                TokenType.EqualEqual => Operators.Equals(left.Value, right.Value),
                TokenType.BangEqual => Operators.NotEquals(left.Value, right.Value),
                TokenType.Less => FoldComparison(left.Value, right.Value, TokenType.Less),
                TokenType.Greater => FoldComparison(left.Value, right.Value, TokenType.Greater),
                TokenType.LessEqual => FoldComparison(left.Value, right.Value, TokenType.LessEqual),
                TokenType.GreaterEqual => FoldComparison(left.Value, right.Value, TokenType.GreaterEqual),
                TokenType.Amp => Operators.BitwiseAnd(left.Value, right.Value),
                TokenType.Pipe => Operators.BitwiseOr(left.Value, right.Value),
                TokenType.Caret => Operators.BitwiseXor(left.Value, right.Value),
                TokenType.LessLess => Operators.LeftShift(left.Value, right.Value),
                TokenType.GreaterGreater => Operators.RightShift(left.Value, right.Value),
                _ => null
            };

            if (result == null) return binary;
            result = ApplyConstantPromotion(result, left.Value, right.Value);
            var folded = BoundLiteralExpr.FromValue(result);
            folded.Span = binary.Span;
            return folded;
        }
        catch
        {
            return binary;
        }
    }

    /// <summary>
    /// ECMA-334 constant expression rules: when one operand is uint/ulong and the other is
    /// a non-negative int literal, the result type should match the unsigned type, not long.
    /// NumericDispatch always promotes uint+int to long, but C# constant expressions preserve uint.
    /// </summary>
    private static object ApplyConstantPromotion(object result, object leftVal, object rightVal)
    {
        if (result is not long longResult) return result;

        if (leftVal is uint && IsNonNegativeInt(rightVal))
            return unchecked((uint)longResult);
        if (rightVal is uint && IsNonNegativeInt(leftVal))
            return unchecked((uint)longResult);
        if (leftVal is ulong || rightVal is ulong)
        {
            if (IsNonNegativeInt(leftVal) || IsNonNegativeInt(rightVal))
                return unchecked((ulong)longResult);
        }

        return result;
    }

    private static bool IsNonNegativeInt(object val)
    {
        return val is int i && i >= 0;
    }

    protected override BoundExpr RevisitLogical(BoundExpr node)
    {
        if (node is not BoundLogicalExpr logical) return node;
        return TryFoldLogical(logical);
    }

    private static BoundExpr TryFoldLogical(BoundLogicalExpr logical)
    {
        if (logical.Left is not BoundLiteralExpr { Value: bool leftVal } ||
            logical.Right is not BoundLiteralExpr { Value: bool rightVal })
            return logical;

        var result = logical.Operator switch
        {
            TokenType.AmpAmp => leftVal && rightVal,
            TokenType.PipePipe => leftVal || rightVal,
            _ => (bool?)null
        };

        if (result == null) return logical;
        var folded = BoundLiteralExpr.FromValue(result.Value);
        folded.Span = logical.Span;
        return folded;
    }

    protected override BoundExpr VisitConditional(BoundConditionalExpr node)
    {
        var rewritten = (BoundConditionalExpr)base.VisitConditional(node);
        if (rewritten.Condition is not BoundLiteralExpr { Value: bool condVal })
            return rewritten;

        var chosen = condVal ? rewritten.ThenBranch : rewritten.ElseBranch;

        // Ternary result type is the common type of both branches, not the chosen branch's type.
        // If the branches have different types, insert a cast to preserve correct semantics.
        if (rewritten.ThenBranch.StaticType != rewritten.ElseBranch.StaticType &&
            rewritten.StaticType != typeof(object) &&
            chosen.StaticType != rewritten.StaticType)
        {
            var cast = new BoundNodes.BoundCastExpr(chosen, rewritten.StaticType, chosen.StaticType, rewritten.StaticType);
            cast.Span = rewritten.Span;
            return cast;
        }

        return chosen;
    }

    private static object? FoldAdd(object left, object right)
    {
        if (left is string || right is string)
            return $"{left}{right}";
        return NumericDispatch.Add(left, right);
    }

    private static object? FoldComparison(object left, object right, TokenType op)
    {
        var options = new CsEvalOptions();
        return op switch
        {
            TokenType.Less => Operators.LessThan(left, right, options),
            TokenType.Greater => Operators.GreaterThan(left, right, options),
            TokenType.LessEqual => Operators.LessThanOrEqual(left, right, options),
            TokenType.GreaterEqual => Operators.GreaterThanOrEqual(left, right, options),
            _ => null
        };
    }
}
