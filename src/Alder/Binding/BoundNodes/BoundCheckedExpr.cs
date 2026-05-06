namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.CheckedExpression, "Checked")]
internal sealed partial record BoundCheckedExpr(
    BoundExpr Expression,
    bool IsChecked,
    BoundType StaticType) : BoundExpr(StaticType);
