namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.RangeExpression, "Range")]
internal sealed partial record BoundRangeExpr(
    BoundExpr? Start,
    BoundExpr? End,
    bool ExclusiveEnd,
    BoundType StaticType) : BoundExpr(StaticType);
