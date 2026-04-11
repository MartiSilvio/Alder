namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.ThrowExpression, "Throw")]
internal sealed partial record BoundThrowExpr(
    BoundExpr? Expression,
    BoundType StaticType) : BoundExpr(StaticType);
