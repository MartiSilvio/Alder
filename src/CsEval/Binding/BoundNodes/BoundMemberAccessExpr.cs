using CsEval.Binding.Plans;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMemberAccessExpr(
    BoundExpr Target,
    string MemberName,
    BoundMemberPlan Plan,
    Type StaticType) : BoundExpr(StaticType);
