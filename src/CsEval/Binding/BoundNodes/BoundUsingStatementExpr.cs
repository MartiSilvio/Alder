namespace CsEval.Binding.BoundNodes;

internal sealed record BoundUsingStatementExpr(
    BoundExpr Resource,
    BoundExpr Body,
    Type StaticType) : BoundExpr(StaticType);
