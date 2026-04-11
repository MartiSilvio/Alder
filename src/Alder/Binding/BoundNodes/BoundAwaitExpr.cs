namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.AwaitExpression, "Await")]
internal sealed partial record BoundAwaitExpr(
    BoundExpr Operand,
    BoundType StaticType) : BoundExpr(StaticType);
