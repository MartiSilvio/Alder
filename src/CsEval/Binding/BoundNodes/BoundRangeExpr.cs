namespace CsEval.Binding.BoundNodes;

internal sealed record BoundRangeExpr(
    BoundExpr Start,
    BoundExpr End,
    bool ExclusiveEnd,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Start); visit(End); }
}
