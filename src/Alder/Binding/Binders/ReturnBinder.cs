using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class ReturnBinder : INodeBinder<ReturnExpr>
{
    public BoundExpr Bind(ReturnExpr expr, BindingContext context, BinderContext binder)
    {
        var value = expr.Value != null ? binder.Bind(expr.Value, context) : null;
        return new BoundReturnExpr(value, value?.StaticType ?? BoundType.Void);
    }
}
