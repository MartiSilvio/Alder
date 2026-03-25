namespace Alder.Binding.BoundNodes;

internal sealed record BoundAsExpr(
    BoundExpr Expression,
    Type TargetType,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.AsOperator;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Expression); }
}
