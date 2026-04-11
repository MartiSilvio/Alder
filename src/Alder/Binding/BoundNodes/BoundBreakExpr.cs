namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.BreakStatement, "Break")]
internal sealed partial record BoundBreakExpr(BoundType StaticType) : BoundExpr(StaticType);
