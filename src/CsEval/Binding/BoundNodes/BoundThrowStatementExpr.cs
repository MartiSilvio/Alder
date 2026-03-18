namespace CsEval.Binding.BoundNodes;

internal sealed record BoundThrowStatementExpr(Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
