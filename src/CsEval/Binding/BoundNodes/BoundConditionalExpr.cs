namespace CsEval.Binding.BoundNodes;

internal sealed record BoundConditionalExpr(
    BoundExpr Condition,
    BoundExpr ThenBranch,
    BoundExpr ElseBranch,
    Type StaticType) : BoundExpr(StaticType);
