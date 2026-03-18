namespace CsEval.Binding.BoundNodes;

internal sealed record BoundAsExpr(
    BoundExpr Expression,
    Type TargetType,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Expression); }
}
