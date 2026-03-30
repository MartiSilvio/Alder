using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(LiteralExpr))]
internal static class LiteralBinder
{
    public static BoundExpr Bind(LiteralExpr expr, BindingContext context, BinderContext binder)
    {
        return BoundLiteralExpr.FromValue(expr.Value);
    }
}
