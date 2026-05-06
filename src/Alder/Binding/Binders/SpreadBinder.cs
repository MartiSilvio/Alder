using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(SpreadExpr))]
internal static class SpreadBinder
{
    public static BoundExpr Bind(SpreadExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        var elementType = ForEachBinder.InferElementType(expression.StaticType.ClrType);
        return new BoundSpreadExpr(expression, new BoundType(elementType));
    }
}
