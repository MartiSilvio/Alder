namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.DynamicIndexAccess, "DynamicIndexAccess")]
internal sealed partial record BoundDynamicIndexAccessExpr(
    BoundExpr Target,
    BoundExpr Index,
    bool NullSafe,
    BoundType StaticType) : BoundExpr(StaticType);
