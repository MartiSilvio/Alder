namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMemberIncrementExpr(
    BoundExpr Target,
    string MemberName,
    bool IsPrefix,
    bool IsIncrement,
    Type StaticType) : BoundExpr(StaticType);
