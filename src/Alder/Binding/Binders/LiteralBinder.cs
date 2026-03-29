using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class LiteralBinder : INodeBinder<LiteralExpr>
{
    public BoundExpr Bind(LiteralExpr expr, BindingContext context, BinderContext binder)
    {
        return BoundLiteralExpr.FromValue(expr.Value);
    }
}
