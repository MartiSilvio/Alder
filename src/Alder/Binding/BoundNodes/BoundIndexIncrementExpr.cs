using Alder.Binding.Plans;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundIndexIncrementExpr(
    BoundExpr Target,
    BoundExpr Index,
    BoundIndexPlan? Plan,
    bool IsPrefix,
    bool IsIncrement,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.IndexIncrement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Index); }
}
