namespace Alder.Binding.BoundNodes;

internal sealed record BoundThrowExpr(
    BoundExpr? Expression,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ThrowExpression;

    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        if (Expression != null) visit(Expression);
    }
}