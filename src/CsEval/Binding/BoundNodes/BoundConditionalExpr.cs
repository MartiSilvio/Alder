namespace CsEval.Binding.BoundNodes;

internal sealed record BoundConditionalExpr(
    BoundExpr Condition,
    BoundExpr ThenBranch,
    BoundExpr ElseBranch,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Condition); visit(ThenBranch); visit(ElseBranch); }
}
