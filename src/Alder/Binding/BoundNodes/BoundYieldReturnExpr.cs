namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.YieldReturnStatement, "YieldReturn")]
internal sealed partial record BoundYieldReturnExpr(
    BoundExpr Value,
    BoundType StaticType) : BoundExpr(StaticType);
