namespace CsEval.Binding.BoundNodes;

internal sealed record BoundThrowExpr(
    BoundExpr Expression,
    Type StaticType) : BoundExpr(StaticType);
