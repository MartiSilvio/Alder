using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.Block, "Block")]
internal sealed partial record BoundBlockExpr(
    ImmutableArray<BoundExpr> Statements,
    BoundExpr? ReturnExpr,
    BoundType StaticType) : BoundExpr(StaticType);
