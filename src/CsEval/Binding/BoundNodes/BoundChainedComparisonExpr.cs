using CsEval.Parsing;
using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundChainedComparisonExpr(
    ImmutableArray<BoundExpr> Operands,
    ImmutableArray<TokenType> Operators,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { foreach (var o in Operands) visit(o); }
}
