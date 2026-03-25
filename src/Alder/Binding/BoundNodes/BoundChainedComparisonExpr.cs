using System.Collections.Immutable;
using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundChainedComparisonExpr(
    ImmutableArray<BoundExpr> Operands,
    ImmutableArray<TokenType> Operators,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ChainedComparisonOperator;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { foreach (var o in Operands) visit(o); }
}
