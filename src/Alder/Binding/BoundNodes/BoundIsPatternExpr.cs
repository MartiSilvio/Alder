using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundIsPatternExpr(
    BoundExpr Expression,
    Pattern Pattern,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.IsPatternExpression;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Expression); }
}
