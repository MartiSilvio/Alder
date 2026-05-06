namespace Alder.Binding.BoundNodes;

// ^expr creates System.Index(expr, fromEnd: true)
[BoundNode(BoundNodeKind.FromEndIndexExpression, "IndexFromEnd")]
internal sealed partial record BoundIndexFromEndExpr(
    BoundExpr Operand,
    BoundType StaticType) : BoundExpr(StaticType);
