namespace CsEval.Binding.BoundNodes;

internal sealed record BoundBreakExpr(Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.BreakStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
