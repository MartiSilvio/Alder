namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMemberNullCoalesceAssignExpr(
    BoundExpr Target,
    string MemberName,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType);
