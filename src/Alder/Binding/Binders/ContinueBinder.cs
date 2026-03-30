using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ContinueExpr))]
internal static class ContinueBinder
{
    public static BoundExpr Bind(ContinueExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundContinueExpr(BoundType.Void);
    }
}
