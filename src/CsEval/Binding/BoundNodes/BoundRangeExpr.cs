namespace CsEval.Binding.BoundNodes;

internal sealed record BoundRangeExpr(
    BoundExpr? Start,
    BoundExpr? End,
    bool ExclusiveEnd,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.RangeExpression;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        if (Start != null) visit(Start);
        if (End != null) visit(End);
    }
}
