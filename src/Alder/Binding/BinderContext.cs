using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding;

internal sealed class BinderContext
{
    private readonly Func<Expr, BindingContext, BoundExpr> _bind;
    private readonly Dictionary<Type, Func<Expr, BindingContext, BinderContext, BoundExpr>> _binders = new();

    internal BinderContext(Func<Expr, BindingContext, BoundExpr> bind)
    {
        _bind = bind;
    }

    internal void Register<TExpr>(INodeBinder<TExpr> binder) where TExpr : Expr
    {
        _binders[typeof(TExpr)] = (expr, ctx, bc) => binder.Bind((TExpr)expr, ctx, bc);
    }

    public BoundExpr Bind(Expr expr, BindingContext context) => _bind(expr, context);

    internal bool TryBind(Expr expr, BindingContext context, out BoundExpr result)
    {
        if (_binders.TryGetValue(expr.GetType(), out var binder))
        {
            result = binder(expr, context, this);
            return true;
        }
        result = default!;
        return false;
    }
}
