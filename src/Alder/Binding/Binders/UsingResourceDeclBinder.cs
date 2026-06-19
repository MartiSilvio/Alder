using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(UsingResourceDeclExpr))]
internal static class UsingResourceDeclBinder
{
    public static BoundExpr Bind(UsingResourceDeclExpr expr, BindingContext context, BinderContext binder)
    {
        return VariableDeclBinder.BindUsingResource(expr, context, binder);
    }
}
