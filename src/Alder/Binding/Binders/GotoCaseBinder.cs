using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(GotoCaseExpr))]
internal static class GotoCaseBinder
{
    public static BoundExpr Bind(GotoCaseExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundGotoCaseExpr(binder.Bind(expr.Value, context), BoundType.Void);
    }
}
