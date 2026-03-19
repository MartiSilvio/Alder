namespace CsEval.Binding.BoundNodes;

internal sealed record BoundContinueExpr(Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ContinueStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
