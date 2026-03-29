using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class LogicalBinder : INodeBinder<LogicalExpr>
{
    public BoundExpr Bind(LogicalExpr expr, BindingContext context, BinderContext binder)
    {
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
            var right = binder.Bind(link.Right, context);
            if (result.HasErrors || right.HasErrors)
            {
                result = new BoundLogicalExpr(link.Op.Type, result, right, new BoundType(typeof(bool))) { Span = link.Span, HasErrors = true };
                continue;
            }
            result = new BoundLogicalExpr(link.Op.Type, result, right, new BoundType(typeof(bool))) { Span = link.Span };
        }

        return result;
    }
}
