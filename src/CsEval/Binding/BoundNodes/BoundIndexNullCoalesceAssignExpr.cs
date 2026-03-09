namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIndexNullCoalesceAssignExpr(
    BoundExpr Target,
    BoundExpr Index,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType);
