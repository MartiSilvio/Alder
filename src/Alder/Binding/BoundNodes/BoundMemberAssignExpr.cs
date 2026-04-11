namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.MemberAssignment, "MemberAssign")]
internal sealed partial record BoundMemberAssignExpr(
    BoundExpr Target,
    string MemberName,
    MemberInfo? ResolvedMember,
    Type? DeclaringType,
    BoundExpr Value,
    BoundType StaticType) : BoundExpr(StaticType);
