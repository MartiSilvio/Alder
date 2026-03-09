namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIndexAssignExpr(
    BoundExpr Target,
    BoundExpr Index,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType);
