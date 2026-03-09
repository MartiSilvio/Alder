namespace CsEval.Binding.BoundNodes;

internal sealed record BoundOutArgExpr(
    string VariableName,
    string? TypeName,
    bool IsDiscard,
    Type StaticType) : BoundExpr(StaticType);
