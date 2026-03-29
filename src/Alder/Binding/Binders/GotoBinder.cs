using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class GotoBinder : INodeBinder<GotoExpr>
{
    public BoundExpr Bind(GotoExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundGotoExpr(expr.Label, BoundType.Void);
    }
}
