namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.IndexNullCoalesceAssignment, "IndexNullCoalesceAssign")]
internal sealed partial record BoundIndexNullCoalesceAssignExpr(
    BoundExpr Target,
    BoundExpr Index,
    BoundExpr Value,
    BoundType StaticType) : BoundExpr(StaticType);
