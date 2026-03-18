namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIdentifierExpr(string Name, Type StaticType, int? LocalId = null) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
