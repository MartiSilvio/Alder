using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(CheckedExpr))]
internal static class CheckedBinder
{
    public static BoundExpr Bind(CheckedExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        return new BoundCheckedExpr(expression, expr.IsChecked, expression.StaticType);
    }
}
