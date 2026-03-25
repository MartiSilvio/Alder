using Alder.Binding.Plans;
using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundMemberCompoundAssignExpr(
    BoundExpr Target,
    string MemberName,
    BoundMemberPlan? Plan,
    TokenType Operator,
    BoundExpr Value,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.MemberCompoundAssignment;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Value); }
}
