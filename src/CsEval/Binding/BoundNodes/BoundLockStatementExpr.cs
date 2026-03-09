namespace CsEval.Binding.BoundNodes;

internal sealed record BoundLockStatementExpr(
    BoundExpr LockObject,
    BoundExpr Body,
    Type StaticType) : BoundExpr(StaticType);
