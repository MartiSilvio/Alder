using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class GotoCaseBinder : INodeBinder<GotoCaseExpr>
{
    public BoundExpr Bind(GotoCaseExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundGotoCaseExpr(binder.Bind(expr.Value, context), BoundType.Void);
    }
}
