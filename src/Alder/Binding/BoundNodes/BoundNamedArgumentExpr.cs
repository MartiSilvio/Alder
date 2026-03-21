namespace Alder.Binding.BoundNodes;

internal sealed record BoundNamedArgumentExpr(
    string Name,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.NamedArgument;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Value); }
}
