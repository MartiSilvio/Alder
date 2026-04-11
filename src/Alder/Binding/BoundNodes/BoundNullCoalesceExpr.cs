namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.NullCoalescingOperator, "NullCoalesce", ChainFlatten = true)]
internal sealed partial record BoundNullCoalesceExpr(
    BoundExpr Left,
    BoundExpr Right,
    BoundType StaticType) : BoundExpr(StaticType);
