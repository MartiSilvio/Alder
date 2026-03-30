using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(DeconstructionExpr))]
internal static class DeconstructionBinder
{
    public static BoundExpr Bind(DeconstructionExpr expr, BindingContext context, BinderContext binder)
    {
        var valueExpression = binder.Bind(expr.ValueExpression, context);
        var variableNames = expr.VariableNames.ToImmutableArray();
        return new BoundDeconstructionExpr(variableNames, valueExpression, valueExpression.StaticType);
    }
}
