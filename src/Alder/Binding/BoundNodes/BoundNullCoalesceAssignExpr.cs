namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.NullCoalescingAssignmentOperator, "NullCoalesceAssign")]
internal sealed partial record BoundNullCoalesceAssignExpr(
    string Name,
    BoundExpr Value,
    BoundType StaticType,
    int? LocalId = null) : BoundExpr(StaticType);
