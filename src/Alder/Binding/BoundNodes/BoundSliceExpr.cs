namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.SliceExpression, "Slice")]
internal sealed partial record BoundSliceExpr(
    BoundExpr Target,
    BoundExpr? Start,
    BoundExpr? End,
    BoundExpr? Step,
    BoundType StaticType) : BoundExpr(StaticType);
