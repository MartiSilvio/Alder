using Alder.Binding.Plans;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundIndexAssignExpr(
    BoundExpr Target,
    BoundExpr Index,
    BoundIndexPlan? Plan,
    BoundExpr Value,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.IndexAssignment;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Index); visit(Value); }
}
