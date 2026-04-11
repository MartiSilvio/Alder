namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.IndexAssignment, "IndexAssign")]
internal sealed partial record BoundIndexAssignExpr(
    BoundExpr Target,
    BoundExpr Index,
    BoundExpr Value,
    BoundType StaticType) : BoundExpr(StaticType);
