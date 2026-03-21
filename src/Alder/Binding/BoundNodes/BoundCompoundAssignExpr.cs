using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundCompoundAssignExpr(
    string Name,
    TokenType Operator,
    BoundExpr Value,
    Type StaticType,
    int? LocalId = null) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.CompoundAssignmentOperator;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Value); }
}
