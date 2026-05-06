using System.Collections.Immutable;
using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.ChainedComparisonOperator, "ChainedComparison")]
internal sealed partial record BoundChainedComparisonExpr(
    ImmutableArray<BoundExpr> Operands,
    ImmutableArray<TokenType> Operators,
    BoundType StaticType) : BoundExpr(StaticType);
