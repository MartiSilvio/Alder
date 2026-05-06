using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(NameofExpr))]
internal static class NameofBinder
{
    public static BoundExpr Bind(NameofExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundLiteralExpr(expr.Name, new BoundType(typeof(string)));
    }
}
