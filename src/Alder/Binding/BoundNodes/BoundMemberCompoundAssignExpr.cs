using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.MemberCompoundAssignment, "MemberCompoundAssign")]
internal sealed partial record BoundMemberCompoundAssignExpr(
    BoundExpr Target,
    string MemberName,
    TokenType Operator,
    BoundExpr Value,
    BoundType StaticType) : BoundExpr(StaticType);
