namespace CsEval.Binding.BoundNodes;

internal sealed record BoundTypedArrayCreationExpr(
    string ElementTypeName,
    BoundExpr Size,
    Type StaticType) : BoundExpr(StaticType);
