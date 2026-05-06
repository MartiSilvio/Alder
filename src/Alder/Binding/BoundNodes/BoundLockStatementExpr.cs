namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.LockStatement, "LockStatement")]
internal sealed partial record BoundLockStatementExpr(
    BoundExpr LockObject,
    BoundExpr Body,
    BoundType StaticType) : BoundExpr(StaticType);
