using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Optimization;

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
            return BoundLiteralExpr.FromValue(result) with { Span = rewritten.Span };
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

        var leftIsNumeric = TypeHelpers.IsArithmetic(left.Value);
        var rightIsNumeric = TypeHelpers.IsArithmetic(right.Value);

        try
        {
            var result = binary.Operator switch
            {
                TokenType.Plus => FoldAdd(left.Value, right.Value, leftIsNumeric, rightIsNumeric),
                TokenType.Minus when leftIsNumeric && rightIsNumeric => NumericDispatch.Subtract(left.Value, right.Value),
                TokenType.Star when leftIsNumeric && rightIsNumeric => NumericDispatch.Multiply(left.Value, right.Value),
                TokenType.Slash when leftIsNumeric && rightIsNumeric => NumericDispatch.Divide(left.Value, right.Value),
                TokenType.Percent when leftIsNumeric && rightIsNumeric => NumericDispatch.Modulo(left.Value, right.Value),
                TokenType.EqualEqual => Operators.Equals(left.Value, right.Value),
                TokenType.BangEqual => Operators.NotEquals(left.Value, right.Value),
                TokenType.Less when leftIsNumeric && rightIsNumeric => FoldComparison(left.Value, right.Value, TokenType.Less),
                TokenType.Greater when leftIsNumeric && rightIsNumeric => FoldComparison(left.Value, right.Value, TokenType.Greater),
                TokenType.LessEqual when leftIsNumeric && rightIsNumeric => FoldComparison(left.Value, right.Value, TokenType.LessEqual),
                TokenType.GreaterEqual when leftIsNumeric && rightIsNumeric => FoldComparison(left.Value, right.Value, TokenType.GreaterEqual),
                TokenType.Amp when leftIsNumeric && rightIsNumeric => Operators.BitwiseAnd(left.Value, right.Value),
                TokenType.Pipe when leftIsNumeric && rightIsNumeric => Operators.BitwiseOr(left.Value, right.Value),
                TokenType.Caret when leftIsNumeric && rightIsNumeric => Operators.BitwiseXor(left.Value, right.Value),
                TokenType.LessLess when leftIsNumeric && rightIsNumeric => Operators.LeftShift(left.Value, right.Value),
                TokenType.GreaterGreater when leftIsNumeric && rightIsNumeric => Operators.RightShift(left.Value, right.Value),
                _ => null
            };

            if (result == null) return binary;
            result = ApplyConstantPromotion(result, left.Value, right.Value);
            return BoundLiteralExpr.FromValue(result) with { Span = binary.Span };
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
        return BoundLiteralExpr.FromValue(result.Value) with { Span = logical.Span };
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
            rewritten.StaticType.ClrType != typeof(object) &&
            chosen.StaticType != rewritten.StaticType)
        {
            var cast = new BoundCastExpr(chosen, rewritten.StaticType.ClrType, chosen.StaticType.ClrType,
                rewritten.StaticType)
            {
                Span = rewritten.Span
            };
            return cast;
        }

        return chosen;
    }

    private static object? FoldAdd(object left, object right, bool leftIsNumeric, bool rightIsNumeric)
    {
        if (left is string || right is string)
            return $"{left}{right}";
        if (!leftIsNumeric || !rightIsNumeric)
            return null;
        return NumericDispatch.Add(left, right);
    }

    private static object? FoldComparison(object left, object right, TokenType op)
    {
        return op switch
        {
            TokenType.Less => Operators.LessThan(left, right, StringComparison.Ordinal),
            TokenType.Greater => Operators.GreaterThan(left, right, StringComparison.Ordinal),
            TokenType.LessEqual => Operators.LessThanOrEqual(left, right, StringComparison.Ordinal),
            TokenType.GreaterEqual => Operators.GreaterThanOrEqual(left, right, StringComparison.Ordinal),
            _ => null
        };
    }
}
