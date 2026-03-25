using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundMultiDimTypedArrayCreationExpr(
    string ElementTypeName,
    ImmutableArray<BoundExpr> Sizes,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.MultiDimTypedArrayCreation;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { foreach (var s in Sizes) visit(s); }
}
