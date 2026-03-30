using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ChainedComparisonExpr))]
internal static class ChainedComparisonBinder
{
    public static BoundExpr Bind(ChainedComparisonExpr expr, BindingContext context, BinderContext binder)
    {
        var operands = expr.Operands
            .Select(operand => binder.Bind(operand, context))
            .ToImmutableArray();
        var operators = expr.Operators
            .Select(@operator => @operator.Type)
            .ToImmutableArray();
        return new BoundChainedComparisonExpr(operands, operators, new BoundType(typeof(bool)));
    }
}
