using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundIndexCompoundAssignExpr(
    BoundExpr Target,
    BoundExpr Index,
    TokenType Operator,
    BoundExpr Value,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.IndexCompoundAssignment;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Target); visit(Index); visit(Value); }
}
