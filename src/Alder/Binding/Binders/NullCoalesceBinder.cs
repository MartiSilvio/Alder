using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(NullCoalesceExpr))]
internal static class NullCoalesceBinder
{
    public static BoundExpr Bind(NullCoalesceExpr expr, BindingContext context, BinderContext binder)
    {
        if (expr.Left is not NullCoalesceExpr)
        {
            var left = binder.Bind(expr.Left, context);
            var right = binder.Bind(expr.Right, context);
            if (left.HasErrors || right.HasErrors)
                return new BoundNullCoalesceExpr(left, right, BoundType.Unknown) { Span = expr.Span, HasErrors = true };
            ValidateLeftOperand(left);
            return new BoundNullCoalesceExpr(left, right, new BoundType(GetNullCoalesceResultType(left.StaticType.ClrType, right.StaticType.ClrType))) { Span = expr.Span };
        }

        var chain = new List<NullCoalesceExpr>();
        Expr leftmost = expr;
        while (leftmost is NullCoalesceExpr nc)
        {
            chain.Add(nc);
            leftmost = nc.Left;
        }

        var result = binder.Bind(leftmost, context);
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var link = chain[i];
            var r = binder.Bind(link.Right, context);
            if (result.HasErrors || r.HasErrors)
            {
                result = new BoundNullCoalesceExpr(result, r, BoundType.Unknown) { Span = link.Span, HasErrors = true };
                continue;
            }
            ValidateLeftOperand(result);
            result = new BoundNullCoalesceExpr(result, r, new BoundType(BinaryBinder.GetCommonType(result.StaticType.ClrType, r.StaticType.ClrType))) { Span = link.Span };
        }

        return result;
    }

    // §12.15 / CS0019: left operand of ?? must be nullable — reference type, nullable value type, or dynamic.
    private static void ValidateLeftOperand(BoundExpr left)
    {
        if (left.StaticType is BoundUnknownType)
            return;
        if (CanYieldNull(left))
            return;

        var type = left.StaticType.ClrType;
        if (type == typeof(object) || !type.IsValueType)
            return;
        if (Nullable.GetUnderlyingType(type) != null)
            return;

        throw new AlderException(
            DiagnosticDescriptors.BadBinaryOps,
            TokenLexemes.GetCanonical(TokenType.QuestionQuestion),
            type.Name,
            type.Name);
    }

    // §12.15: the result type of A ?? B unwraps A's Nullable<T> when A is a nullable value
    // type whose underlying T matches (or implicitly converts to) B's type.
    private static Type GetNullCoalesceResultType(Type leftType, Type rightType)
    {
        var leftUnderlying = Nullable.GetUnderlyingType(leftType);
        if (leftUnderlying != null)
        {
            if (leftUnderlying == rightType)
                return rightType;
            if (TypeHelpers.CanImplicitlyConvert(leftUnderlying, rightType))
                return rightType;
            if (TypeHelpers.CanImplicitlyConvert(rightType, leftUnderlying))
                return leftUnderlying;
        }
        return BinaryBinder.GetCommonType(leftType, rightType);
    }

    // §12.8.8: a null-conditional member/index access lifts value-type results to nullable,
    // so `s?.Length` (int under the hood) can legitimately feed `?? default`.
    private static bool CanYieldNull(BoundExpr expr) => expr switch
    {
        BoundPropertyAccessExpr p => p.NullSafe,
        BoundFieldAccessExpr f => f.NullSafe,
        BoundDynamicMemberAccessExpr d => d.NullSafe,
        BoundResolvedIndexAccessExpr r => r.NullSafe,
        BoundDynamicIndexAccessExpr d => d.NullSafe,
        BoundResolvedMultiDimIndexAccessExpr r => r.NullSafe,
        BoundDynamicMultiDimIndexAccessExpr d => d.NullSafe,
        _ => false
    };
}
