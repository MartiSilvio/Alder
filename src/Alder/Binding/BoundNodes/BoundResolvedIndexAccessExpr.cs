namespace Alder.Binding.BoundNodes;

internal sealed record BoundResolvedIndexAccessExpr(
    BoundExpr Target,
    BoundExpr Index,
    Type TargetType,
    Type ResultType,
    bool IsDirectCollectionAccess,
    bool NullSafe,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ResolvedIndexAccess;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Index); }
}
