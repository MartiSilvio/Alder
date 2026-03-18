namespace CsEval.Binding.BoundNodes;

internal sealed record BoundPipelineExpr(
    BoundExpr Left,
    BoundExpr Right,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Left); visit(Right); }
}
