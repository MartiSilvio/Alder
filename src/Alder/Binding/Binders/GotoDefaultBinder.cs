using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class GotoDefaultBinder : INodeBinder<GotoDefaultExpr>
{
    public BoundExpr Bind(GotoDefaultExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundGotoDefaultExpr(BoundType.Void);
    }
}
