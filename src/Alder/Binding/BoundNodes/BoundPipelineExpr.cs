namespace Alder.Binding.BoundNodes;

internal sealed record BoundPipelineExpr(
    BoundExpr Left,
    BoundExpr Right,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.PipelineExpression;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Left); visit(Right); }
}
