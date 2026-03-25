namespace Alder.Binding.BoundNodes;

internal sealed record BoundNullCoalesceExpr(
    BoundExpr Left,
    BoundExpr Right,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.NullCoalescingOperator;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Left); visit(Right); }
}
