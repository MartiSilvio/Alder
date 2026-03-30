using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(OutArgExpr))]
internal static class OutArgBinder
{
    public static BoundExpr Bind(OutArgExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundOutArgExpr(expr.VariableName, expr.TypeName, expr.IsDiscard, BoundType.Unknown);
    }
}
