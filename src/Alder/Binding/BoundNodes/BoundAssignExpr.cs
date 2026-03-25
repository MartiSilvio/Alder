namespace Alder.Binding.BoundNodes;

internal sealed record BoundAssignExpr(
    string Name,
    BoundExpr Value,
    BoundType StaticType,
    int? LocalId = null) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.AssignmentOperator;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Value); }
}
