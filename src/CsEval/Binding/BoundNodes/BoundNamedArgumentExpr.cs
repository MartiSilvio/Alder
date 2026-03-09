namespace CsEval.Binding.BoundNodes;

internal sealed record BoundNamedArgumentExpr(
    string Name,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType);
