namespace Alder.Binding.BoundNodes;

internal sealed record BoundYieldBreakExpr() : BoundExpr(BoundType.Void)
{
    internal override BoundNodeKind Kind => BoundNodeKind.YieldBreakStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
