namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMemberIncrementExpr(
    BoundExpr Target,
    string MemberName,
    bool IsPrefix,
    bool IsIncrement,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.MemberIncrement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); }
}
