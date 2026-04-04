using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(GotoExpr))]
internal static class GotoBinder
{
    public static BoundExpr Bind(GotoExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundGotoExpr(expr, BoundType.Void);
    }
}
