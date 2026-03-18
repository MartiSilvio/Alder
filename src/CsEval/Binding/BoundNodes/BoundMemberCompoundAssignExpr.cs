using CsEval.Parsing;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMemberCompoundAssignExpr(
    BoundExpr Target,
    string MemberName,
    TokenType Operator,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Value); }
}
