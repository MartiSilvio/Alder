using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundDynamicMultiDimIndexAccessExpr(
    BoundExpr Target,
    ImmutableArray<BoundExpr> Indices,
    bool NullSafe,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.DynamicMultiDimIndexAccess;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Target);
        foreach (var i in Indices) visit(i);
    }
}
