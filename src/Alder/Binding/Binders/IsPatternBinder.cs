using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(IsPatternExpr))]
internal static class IsPatternBinder
{
    public static BoundExpr Bind(IsPatternExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        return new BoundIsPatternExpr(expression, expr.Pattern, new BoundType(typeof(bool)));
    }
}
