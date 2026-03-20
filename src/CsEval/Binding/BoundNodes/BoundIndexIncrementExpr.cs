using CsEval.Binding.Plans;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIndexIncrementExpr(
    BoundExpr Target,
    BoundExpr Index,
    BoundIndexPlan? Plan,
    bool IsPrefix,
    bool IsIncrement,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.IndexIncrement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Index); }
}
