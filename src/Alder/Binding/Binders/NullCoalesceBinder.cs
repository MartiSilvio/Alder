using Alder.Binding.BoundNodes;
using Alder.Parsing;

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
            return new BoundNullCoalesceExpr(left, right, new BoundType(BinaryBinder.GetCommonType(left.StaticType.ClrType, right.StaticType.ClrType))) { Span = expr.Span };
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
            result = new BoundNullCoalesceExpr(result, r, new BoundType(BinaryBinder.GetCommonType(result.StaticType.ClrType, r.StaticType.ClrType))) { Span = link.Span };
        }

        return result;
    }
}
