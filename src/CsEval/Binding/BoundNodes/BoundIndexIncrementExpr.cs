namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIndexIncrementExpr(
    BoundExpr Target,
    BoundExpr Index,
    bool IsPrefix,
    bool IsIncrement,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Index); }
}
