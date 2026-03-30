using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding;

internal sealed class BinderContext
{
    private readonly Func<Expr, BindingContext, BoundExpr> _bind;

    internal BinderContext(Func<Expr, BindingContext, BoundExpr> bind)
    {
        _bind = bind;
    }

    public BoundExpr Bind(Expr expr, BindingContext context) => _bind(expr, context);
}
