using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(NewExpr))]
internal static class NewExprBinder
{
    public static BoundExpr Bind(NewExpr expr, BindingContext context, BinderContext binder)
    {
        return binder.Bind(expr.Initializer, context);
    }
}
