namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.YieldBreakStatement, "YieldBreak")]
internal sealed partial record BoundYieldBreakExpr() : BoundExpr(BoundType.Void);
