using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.TupleLiteral, "Tuple")]
internal sealed partial record BoundTupleExpr(
    ImmutableArray<BoundExpr> Elements,
    ImmutableArray<string?> ElementNames,
    BoundType StaticType) : BoundExpr(StaticType);
