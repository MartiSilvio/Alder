namespace Alder.Binding.BoundNodes;

internal sealed record BoundIdentifierExpr(string Name, BoundType StaticType, int? LocalId = null) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.Identifier;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
