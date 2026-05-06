namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.AssignmentOperator, "Assign")]
internal sealed partial record BoundAssignExpr(
    string Name,
    BoundExpr Value,
    BoundType StaticType,
    int? LocalId = null) : BoundExpr(StaticType);
