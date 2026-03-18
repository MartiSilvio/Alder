namespace CsEval.Binding.BoundNodes;

internal sealed record BoundNullCoalesceAssignExpr(
    string Name,
    BoundExpr Value,
    Type StaticType,
    int? LocalId = null) : BoundExpr(StaticType);
