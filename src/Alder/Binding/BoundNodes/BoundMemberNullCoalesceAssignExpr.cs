using Alder.Binding.Plans;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundMemberNullCoalesceAssignExpr(
    BoundExpr Target,
    string MemberName,
    BoundMemberPlan? Plan,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.MemberNullCoalesceAssignment;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Value); }
}
