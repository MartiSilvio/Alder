namespace Alder.Binding.BoundNodes;

internal sealed record BoundYieldReturnExpr(
    BoundExpr Value,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.YieldReturnStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) => visit(Value);
}
