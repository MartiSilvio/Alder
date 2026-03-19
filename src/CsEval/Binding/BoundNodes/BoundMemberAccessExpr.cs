using CsEval.Binding.Plans;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMemberAccessExpr(
    BoundExpr Target,
    string MemberName,
    bool NullSafe,
    BoundMemberPlan? Plan,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.MemberAccess;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); }
}
