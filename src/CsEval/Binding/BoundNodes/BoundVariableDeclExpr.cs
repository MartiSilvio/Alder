namespace CsEval.Binding.BoundNodes;

internal sealed record BoundVariableDeclExpr(
    string Name,
    BoundExpr Initializer,
    Type? DeclaredType,
    Type StaticType) : BoundExpr(StaticType);
