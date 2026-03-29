using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundArrayAllocationExpr(
    Type ElementType,
    ImmutableArray<BoundExpr> Sizes,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ArrayAllocation;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { foreach (var s in Sizes) visit(s); }
}
