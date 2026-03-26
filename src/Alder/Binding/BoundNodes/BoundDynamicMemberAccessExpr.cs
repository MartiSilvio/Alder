namespace Alder.Binding.BoundNodes;

internal sealed record BoundDynamicMemberAccessExpr(
    BoundExpr Target,
    string MemberName,
    bool NullSafe,
    BoundType StaticType) : BoundMemberAccessBase(Target, MemberName, NullSafe, StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.DynamicMemberAccess;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); }
}
