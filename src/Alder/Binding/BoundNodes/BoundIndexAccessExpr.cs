using Alder.Binding.Plans;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundIndexAccessExpr(
    BoundExpr Target,
    BoundExpr Index,
    BoundIndexPlan? Plan,
    bool NullSafe,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.IndexerAccess;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Index); }
}
