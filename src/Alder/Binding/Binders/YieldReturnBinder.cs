using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(YieldReturnExpr))]
internal static class YieldReturnBinder
{
    public static BoundExpr Bind(YieldReturnExpr expr, BindingContext context, BinderContext binder)
    {
        var value = binder.Bind(expr.Value, context);
        return new BoundYieldReturnExpr(value, value.StaticType);
    }
}
