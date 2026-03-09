namespace CsEval.Binding.BoundNodes;

internal sealed record BoundSliceExpr(
    BoundExpr Target,
    BoundExpr? Start,
    BoundExpr? End,
    BoundExpr? Step,
    Type StaticType) : BoundExpr(StaticType);
