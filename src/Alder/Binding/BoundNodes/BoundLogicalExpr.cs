using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundLogicalExpr(
    TokenType Operator,
    BoundExpr Left,
    BoundExpr Right,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.LogicalOperator;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Left); visit(Right); }
}
