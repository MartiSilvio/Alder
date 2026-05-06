namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.ContinueStatement, "Continue")]
internal sealed partial record BoundContinueExpr(BoundType StaticType) : BoundExpr(StaticType);
