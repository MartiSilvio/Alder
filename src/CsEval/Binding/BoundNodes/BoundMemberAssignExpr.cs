namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMemberAssignExpr(
    BoundExpr Target,
    string MemberName,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.MemberAssignment;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Value); }
}
