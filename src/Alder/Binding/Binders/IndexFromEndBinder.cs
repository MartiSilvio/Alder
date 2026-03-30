using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(IndexFromEndExpr))]
internal static class IndexFromEndBinder
{
    public static BoundExpr Bind(IndexFromEndExpr expr, BindingContext context, BinderContext binder)
    {
        var operand = binder.Bind(expr.Operand, context);
        return new BoundIndexFromEndExpr(operand, new BoundType(typeof(Index)));
    }
}
