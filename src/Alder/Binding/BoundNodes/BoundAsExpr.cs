namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.AsOperator, "As")]
internal sealed partial record BoundAsExpr(
    BoundExpr Expression,
    Type TargetType,
    BoundType StaticType) : BoundExpr(StaticType);
