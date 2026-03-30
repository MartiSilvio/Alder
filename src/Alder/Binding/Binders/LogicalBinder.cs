using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(LogicalExpr))]
internal static class LogicalBinder
{
    public static BoundExpr Bind(LogicalExpr expr, BindingContext context, BinderContext binder)
    {
        if (expr.Left is not LogicalExpr)
        {
            var left = binder.Bind(expr.Left, context);
            var right = binder.Bind(expr.Right, context);
            if (left.HasErrors || right.HasErrors)
                return new BoundLogicalExpr(expr.Op.Type, left, right, new BoundType(typeof(bool))) { Span = expr.Span, HasErrors = true };
            return new BoundLogicalExpr(expr.Op.Type, left, right, new BoundType(typeof(bool))) { Span = expr.Span };
        }

        var chain = new List<LogicalExpr>();
        Expr leftmost = expr;
        while (leftmost is LogicalExpr l)
        {
            chain.Add(l);
            leftmost = l.Left;
        }

        var result = binder.Bind(leftmost, context);
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var link = chain[i];
            var r = binder.Bind(link.Right, context);
            if (result.HasErrors || r.HasErrors)
            {
                result = new BoundLogicalExpr(link.Op.Type, result, r, new BoundType(typeof(bool))) { Span = link.Span, HasErrors = true };
                continue;
            }
            result = new BoundLogicalExpr(link.Op.Type, result, r, new BoundType(typeof(bool))) { Span = link.Span };
        }

        return result;
    }
}
