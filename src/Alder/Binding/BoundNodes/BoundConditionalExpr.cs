namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.ConditionalOperator, "Conditional")]
internal sealed partial record BoundConditionalExpr(
    BoundExpr Condition,
    BoundExpr ThenBranch,
    BoundExpr ElseBranch,
    BoundType StaticType) : BoundExpr(StaticType);
