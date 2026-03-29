using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class SpreadBinder : INodeBinder<SpreadExpr>
{
    public BoundExpr Bind(SpreadExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        var elementType = ForEachBinder.InferElementType(expression.StaticType.ClrType);
        return new BoundSpreadExpr(expression, new BoundType(elementType));
    }
}
