namespace Alder.Binding.BoundNodes;

internal sealed record BoundConditionalExpr(
    BoundExpr Condition,
    BoundExpr ThenBranch,
    BoundExpr ElseBranch,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ConditionalOperator;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Condition); visit(ThenBranch); visit(ElseBranch); }
}
