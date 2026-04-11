using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.DynamicMultiDimIndexAccess, "DynamicMultiDimIndexAccess")]
internal sealed partial record BoundDynamicMultiDimIndexAccessExpr(
    BoundExpr Target,
    ImmutableArray<BoundExpr> Indices,
    bool NullSafe,
    BoundType StaticType) : BoundExpr(StaticType);
