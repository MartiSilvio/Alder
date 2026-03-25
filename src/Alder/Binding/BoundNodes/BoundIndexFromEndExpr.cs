namespace Alder.Binding.BoundNodes;

// ^expr — creates System.Index(expr, fromEnd: true)
internal sealed record BoundIndexFromEndExpr(
    BoundExpr Operand,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.FromEndIndexExpression;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Operand); }
}
