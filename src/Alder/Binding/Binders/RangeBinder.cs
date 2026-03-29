using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class RangeBinder : INodeBinder<RangeExpr>
{
    public BoundExpr Bind(RangeExpr expr, BindingContext context, BinderContext binder)
    {
        var start = expr.Start != null ? binder.Bind(expr.Start, context) : null;
        var end = expr.End != null ? binder.Bind(expr.End, context) : null;
        var resultType = start == null || end == null ? typeof(Range) : typeof(IEnumerable<int>);
        return new BoundRangeExpr(start, end, expr.ExclusiveEnd, new BoundType(resultType));
    }
}
