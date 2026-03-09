namespace CsEval.Binding.BoundNodes;

internal sealed record BoundCastExpr(
    BoundExpr Expression,
    Type TargetType,
    Type? SourceStaticType,
    Type StaticType) : BoundExpr(StaticType);
