namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.MemberNullCoalesceAssignment, "MemberNullCoalesceAssign")]
internal sealed partial record BoundMemberNullCoalesceAssignExpr(
    BoundExpr Target,
    string MemberName,
    BoundExpr Value,
    BoundType StaticType) : BoundExpr(StaticType);
