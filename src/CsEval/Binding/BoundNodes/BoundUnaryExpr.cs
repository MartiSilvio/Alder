using CsEval.Parsing;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundUnaryExpr(
    TokenType Operator,
    BoundExpr Operand,
    Type StaticType) : BoundExpr(StaticType)
{
    internal Type? PromotedType { get; init; }
    internal override BoundNodeKind Kind => BoundNodeKind.UnaryOperator;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Operand); }
}
