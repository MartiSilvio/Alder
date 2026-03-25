namespace Alder.Binding.BoundNodes;

internal sealed record BoundTypedArrayCreationExpr(
    string ElementTypeName,
    BoundExpr Size,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.TypedArrayCreation;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Size); }
}
