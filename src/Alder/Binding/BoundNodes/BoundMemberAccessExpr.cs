using Alder.Binding.Plans;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundMemberAccessExpr(
    BoundExpr Target,
    string MemberName,
    bool NullSafe,
    BoundMemberPlan? Plan,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.MemberAccess;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); }
}
