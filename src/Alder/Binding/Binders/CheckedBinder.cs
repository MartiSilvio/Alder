using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class CheckedBinder : INodeBinder<CheckedExpr>
{
    public BoundExpr Bind(CheckedExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        return new BoundCheckedExpr(expression, expr.IsChecked, expression.StaticType);
    }
}
