using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class OutArgBinder : INodeBinder<OutArgExpr>
{
    public BoundExpr Bind(OutArgExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundOutArgExpr(expr.VariableName, expr.TypeName, expr.IsDiscard, BoundType.Unknown);
    }
}
