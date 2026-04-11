namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.ReturnStatement, "Return")]
internal sealed partial record BoundReturnExpr(
    BoundExpr? Value,
    BoundType StaticType) : BoundExpr(StaticType);
