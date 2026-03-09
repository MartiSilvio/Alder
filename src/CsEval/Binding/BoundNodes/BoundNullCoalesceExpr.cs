namespace CsEval.Binding.BoundNodes;

internal sealed record BoundNullCoalesceExpr(
    BoundExpr Left,
    BoundExpr Right,
    Type StaticType) : BoundExpr(StaticType);
