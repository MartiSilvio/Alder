namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIndexNullCoalesceAssignExpr(
    BoundExpr Target,
    BoundExpr Index,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.IndexNullCoalesceAssignment;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Index); visit(Value); }
}
