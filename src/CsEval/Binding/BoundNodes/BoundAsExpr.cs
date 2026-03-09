namespace CsEval.Binding.BoundNodes;

internal sealed record BoundAsExpr(
    BoundExpr Expression,
    Type TargetType,
    Type StaticType) : BoundExpr(StaticType);
