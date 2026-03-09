namespace CsEval.Binding.BoundNodes;

internal sealed record BoundContinueExpr(Type StaticType) : BoundExpr(StaticType);
