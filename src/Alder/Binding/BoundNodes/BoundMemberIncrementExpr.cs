namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.MemberIncrement, "MemberIncrement")]
internal sealed partial record BoundMemberIncrementExpr(
    BoundExpr Target,
    string MemberName,
    bool IsPrefix,
    bool IsIncrement,
    BoundType StaticType) : BoundExpr(StaticType);
