using CsEval.Parsing;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundBinaryExpr(
    TokenType Operator,
    BoundExpr Left,
    BoundExpr Right,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.BinaryOperator;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Left); visit(Right); }
}
