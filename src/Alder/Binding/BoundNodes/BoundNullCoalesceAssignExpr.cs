namespace Alder.Binding.BoundNodes;

internal sealed record BoundNullCoalesceAssignExpr(
    string Name,
    BoundExpr Value,
    BoundType StaticType,
    int? LocalId = null) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.NullCoalescingAssignmentOperator;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Value); }
}
