using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.ArrayAllocation, "ArrayAllocation")]
internal sealed partial record BoundArrayAllocationExpr(
    Type ElementType,
    ImmutableArray<BoundExpr> Sizes,
    BoundType StaticType) : BoundExpr(StaticType);
