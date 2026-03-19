namespace CsEval.Binding.BoundNodes;

internal sealed record BoundUsingStatementExpr(
    BoundExpr Resource,
    BoundExpr Body,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.UsingStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Resource); visit(Body); }
}
