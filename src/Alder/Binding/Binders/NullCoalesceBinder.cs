using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class NullCoalesceBinder : INodeBinder<NullCoalesceExpr>
{
    public BoundExpr Bind(NullCoalesceExpr expr, BindingContext context, BinderContext binder)
    {
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
            var right = binder.Bind(link.Right, context);
            if (result.HasErrors || right.HasErrors)
            {
                result = new BoundNullCoalesceExpr(result, right, BoundType.Unknown) { Span = link.Span, HasErrors = true };
                continue;
            }
            result = new BoundNullCoalesceExpr(result, right, new BoundType(BinaryBinder.GetCommonType(result.StaticType.ClrType, right.StaticType.ClrType))) { Span = link.Span };
        }

        return result;
    }
}
