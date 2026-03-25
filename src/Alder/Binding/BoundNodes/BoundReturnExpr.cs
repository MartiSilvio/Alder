namespace Alder.Binding.BoundNodes;

internal sealed record BoundReturnExpr(
    BoundExpr? Value,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { if (Value != null) visit(Value); }
}
