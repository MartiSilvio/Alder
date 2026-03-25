namespace Alder.Binding.BoundNodes;

internal sealed record BoundContinueExpr(BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ContinueStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
