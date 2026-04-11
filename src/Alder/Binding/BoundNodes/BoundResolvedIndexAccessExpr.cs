namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.ResolvedIndexAccess, "ResolvedIndexAccess")]
internal sealed partial record BoundResolvedIndexAccessExpr(
    BoundExpr Target,
    BoundExpr Index,
    Type TargetType,
    Type ResultType,
    bool IsDirectCollectionAccess,
    bool NullSafe,
    BoundType StaticType) : BoundExpr(StaticType);
