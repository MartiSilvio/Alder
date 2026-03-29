using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class IndexFromEndBinder : INodeBinder<IndexFromEndExpr>
{
    public BoundExpr Bind(IndexFromEndExpr expr, BindingContext context, BinderContext binder)
    {
        var operand = binder.Bind(expr.Operand, context);
        return new BoundIndexFromEndExpr(operand, new BoundType(typeof(Index)));
    }
}
