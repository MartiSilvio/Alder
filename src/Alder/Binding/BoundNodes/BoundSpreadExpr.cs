namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.SpreadElement, "Spread")]
internal sealed partial record BoundSpreadExpr(
    BoundExpr Expression,
    BoundType StaticType) : BoundExpr(StaticType);
