using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ReturnExpr))]
internal static class ReturnBinder
{
    public static BoundExpr Bind(ReturnExpr expr, BindingContext context, BinderContext binder)
    {
        var value = expr.Value != null ? binder.Bind(expr.Value, context) : null;
        return new BoundReturnExpr(value, value?.StaticType ?? BoundType.Void);
    }
}
