using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(YieldBreakExpr))]
internal static class YieldBreakBinder
{
    public static BoundExpr Bind(YieldBreakExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundYieldBreakExpr();
    }
}
