namespace CsEval.Binding.BoundNodes;

internal sealed record BoundCastExpr(
    BoundExpr Expression,
    Type TargetType,
    Type? SourceStaticType,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Expression); }
}
