namespace CsEval.Binding.BoundNodes;

internal sealed record BoundLockStatementExpr(
    BoundExpr LockObject,
    BoundExpr Body,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.LockStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(LockObject); visit(Body); }
}
