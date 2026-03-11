namespace CsEval.Binding.BoundNodes;

internal sealed record BoundVariableDeclExpr(
    string Name,
    BoundExpr Initializer,
    Type? DeclaredType,
    Type StaticType,
    bool IsConst = false) : BoundExpr(StaticType);
