namespace CsEval.Binding.BoundNodes;

internal sealed record BoundCheckedExpr(
    BoundExpr Expression,
    bool IsChecked,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Expression); }
}
