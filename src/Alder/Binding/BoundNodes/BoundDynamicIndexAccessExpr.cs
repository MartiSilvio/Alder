namespace Alder.Binding.BoundNodes;

internal sealed record BoundDynamicIndexAccessExpr(
    BoundExpr Target,
    BoundExpr Index,
    bool NullSafe,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.DynamicIndexAccess;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Index); }
}
