using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(GotoDefaultExpr))]
internal static class GotoDefaultBinder
{
    public static BoundExpr Bind(GotoDefaultExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundGotoDefaultExpr(BoundType.Void);
    }
}
