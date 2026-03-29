using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class BreakBinder : INodeBinder<BreakExpr>
{
    public BoundExpr Bind(BreakExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundBreakExpr(BoundType.Void);
    }
}
