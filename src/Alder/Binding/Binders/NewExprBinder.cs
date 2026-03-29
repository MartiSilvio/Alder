using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class NewExprBinder : INodeBinder<NewExpr>
{
    public BoundExpr Bind(NewExpr expr, BindingContext context, BinderContext binder)
    {
        return binder.Bind(expr.Initializer, context);
    }
}
