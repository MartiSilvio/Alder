namespace Alder.Binding.BoundNodes;

internal sealed record BoundBreakExpr(BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.BreakStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
