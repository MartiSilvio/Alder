namespace CsEval.Binding.BoundNodes;

internal sealed record BoundBreakExpr(Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
