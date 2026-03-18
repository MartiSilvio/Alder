namespace CsEval.Binding.BoundNodes;

internal sealed record BoundSliceExpr(
    BoundExpr Target,
    BoundExpr? Start,
    BoundExpr? End,
    BoundExpr? Step,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Target);
        if (Start != null) visit(Start);
        if (End != null) visit(End);
        if (Step != null) visit(Step);
    }
}
