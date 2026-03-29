using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class DeconstructionBinder : INodeBinder<DeconstructionExpr>
{
    public BoundExpr Bind(DeconstructionExpr expr, BindingContext context, BinderContext binder)
    {
        var valueExpression = binder.Bind(expr.ValueExpression, context);
        var variableNames = expr.VariableNames.ToImmutableArray();
        return new BoundDeconstructionExpr(variableNames, valueExpression, valueExpression.StaticType);
    }
}
