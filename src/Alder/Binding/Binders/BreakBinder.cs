using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(BreakExpr))]
internal static class BreakBinder
{
    public static BoundExpr Bind(BreakExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundBreakExpr(BoundType.Void);
    }
}
