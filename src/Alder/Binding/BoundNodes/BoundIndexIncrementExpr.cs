namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.IndexIncrement, "IndexIncrement")]
internal sealed partial record BoundIndexIncrementExpr(
    BoundExpr Target,
    BoundExpr Index,
    bool IsPrefix,
    bool IsIncrement,
    BoundType StaticType) : BoundExpr(StaticType);
