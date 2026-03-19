using CsEval.Parsing;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIsPatternExpr(
    BoundExpr Expression,
    Pattern Pattern,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.IsPatternExpression;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Expression); }
}
