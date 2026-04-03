namespace Alder.Binding.BoundNodes;

internal sealed record BoundAwaitExpr(
    BoundExpr Operand,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.AwaitExpression;
    internal override void EnumerateChildren(Action<BoundExpr> visit) => visit(Operand);
}
