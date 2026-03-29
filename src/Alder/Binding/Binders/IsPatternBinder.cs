using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class IsPatternBinder : INodeBinder<IsPatternExpr>
{
    public BoundExpr Bind(IsPatternExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        return new BoundIsPatternExpr(expression, expr.Pattern, new BoundType(typeof(bool)));
    }
}
