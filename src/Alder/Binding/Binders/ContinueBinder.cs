using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class ContinueBinder : INodeBinder<ContinueExpr>
{
    public BoundExpr Bind(ContinueExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundContinueExpr(BoundType.Void);
    }
}
