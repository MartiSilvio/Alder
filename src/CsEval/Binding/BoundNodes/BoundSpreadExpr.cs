namespace CsEval.Binding.BoundNodes;

internal sealed record BoundSpreadExpr(
    BoundExpr Expression,
    Type StaticType) : BoundExpr(StaticType);
