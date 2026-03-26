using System.Collections.Immutable;
using Alder.Binding.Plans;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundMultiDimIndexAccessExpr(
    BoundExpr Target,
    ImmutableArray<BoundExpr> Indices,
    BoundMultiDimIndexPlan? Plan,
    bool NullSafe,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.MultiDimIndexAccess;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Target);
        foreach (var i in Indices) visit(i);
    }
}
