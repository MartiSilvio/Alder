using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(DeconstructionExpr))]
internal static class DeconstructionBinder
{
    public static BoundExpr Bind(DeconstructionExpr expr, BindingContext context, BinderContext binder)
    {
        var valueExpression = binder.Bind(expr.ValueExpression, context);
        return new BoundDeconstructionExpr(expr, valueExpression, valueExpression.StaticType);
    }
}
