using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Optimization;

internal sealed class ConversionInsertionPass : BoundExprRewriter
{
    protected override BoundExpr RevisitBinary(BoundExpr node)
    {
        if (node is not BoundBinaryExpr binary) return node;

        // Strict equality/inequality compare types directly -- promotion would defeat the purpose
        if (binary.Operator is TokenType.EqualEqualEqual or TokenType.BangEqualEqual)
            return binary;

        var leftType = binary.Left.StaticType.ClrType;
        var rightType = binary.Right.StaticType.ClrType;

        if (leftType == typeof(object) || rightType == typeof(object))
            return binary;
        if (leftType == rightType)
            return binary;

        var promoted = TypeHelpers.TryGetBinaryNumericPromotionType(leftType, rightType);
        if (promoted == null) return binary;

        var newLeft = leftType != promoted ? InsertCast(binary.Left, promoted) : binary.Left;
        var newRight = rightType != promoted ? InsertCast(binary.Right, promoted) : binary.Right;

        if (ReferenceEquals(newLeft, binary.Left) && ReferenceEquals(newRight, binary.Right))
            return binary;

        var rewritten = binary with { Left = newLeft, Right = newRight, Span = binary.Span };
        if (binary.HasErrors) rewritten.HasErrors = true;
        if (binary.Diagnostic != null) rewritten.Diagnostic = binary.Diagnostic;
        return rewritten;
    }

    private static BoundExpr InsertCast(BoundExpr expr, Type targetType)
    {
        var cast = new BoundCastExpr(expr, targetType, expr.StaticType.ClrType, new BoundType(targetType))
        {
            Span = expr.Span
        };
        return cast;
    }
}
