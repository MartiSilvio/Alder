namespace CsEval.Binding.BoundNodes;

internal sealed record BoundThrowStatementExpr(Type StaticType) : BoundExpr(StaticType);
