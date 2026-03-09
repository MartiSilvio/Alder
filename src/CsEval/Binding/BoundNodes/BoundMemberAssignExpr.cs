namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMemberAssignExpr(
    BoundExpr Target,
    string MemberName,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType);
