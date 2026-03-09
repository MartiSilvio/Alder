namespace CsEval.Binding.BoundNodes;

internal sealed record BoundReturnExpr(
    BoundExpr? Value,
    Type StaticType) : BoundExpr(StaticType);
