namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIdentifierExpr(string Name, Type StaticType) : BoundExpr(StaticType);
