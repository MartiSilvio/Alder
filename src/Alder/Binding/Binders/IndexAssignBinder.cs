using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(IndexAssignExpr))]
internal static class IndexAssignBinder
{
    public static BoundExpr Bind(IndexAssignExpr expr, BindingContext context, BinderContext binder)
    {
        var target = binder.Bind(expr.Object, context);
        var index = binder.Bind(expr.Index, context);
        var value = binder.Bind(expr.Value, context);
        return new BoundIndexAssignExpr(target, index, value, value.StaticType);
    }
}
