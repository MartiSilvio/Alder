using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(NamedArgumentExpr))]
internal static class NamedArgumentBinder
{
    public static BoundExpr Bind(NamedArgumentExpr expr, BindingContext context, BinderContext binder)
    {
        var value = binder.Bind(expr.Value, context);
        return new BoundNamedArgumentExpr(expr.Name.Lexeme, value, BoundType.Unknown);
    }
}
