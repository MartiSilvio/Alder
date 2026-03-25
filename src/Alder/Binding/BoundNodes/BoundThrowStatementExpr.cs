namespace Alder.Binding.BoundNodes;

internal sealed record BoundThrowStatementExpr(BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ThrowStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
