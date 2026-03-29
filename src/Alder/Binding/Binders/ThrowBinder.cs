using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class ThrowBinder : INodeBinder<ThrowExpr>
{
    public BoundExpr Bind(ThrowExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        return new BoundThrowExpr(expression, BoundType.Void);
    }
}
