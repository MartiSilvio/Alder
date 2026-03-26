namespace Alder.Binding.BoundNodes;

internal sealed record BoundMemberAssignExpr(
    BoundExpr Target,
    string MemberName,
    MemberInfo? ResolvedMember,
    Type? DeclaringType,
    BoundExpr Value,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.MemberAssignment;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Value); }
}
