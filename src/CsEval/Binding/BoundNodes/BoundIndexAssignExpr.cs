namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIndexAssignExpr(
    BoundExpr Target,
    BoundExpr Index,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.IndexAssignment;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Index); visit(Value); }
}
