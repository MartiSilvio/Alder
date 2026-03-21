namespace Alder.Binding.BoundNodes;

internal sealed record BoundSpreadExpr(
    BoundExpr Expression,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.SpreadElement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Expression); }
}
