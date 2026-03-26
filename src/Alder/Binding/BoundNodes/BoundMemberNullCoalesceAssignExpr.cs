namespace Alder.Binding.BoundNodes;

internal sealed record BoundMemberNullCoalesceAssignExpr(
    BoundExpr Target,
    string MemberName,
    BoundExpr Value,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.MemberNullCoalesceAssignment;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Value); }
}
