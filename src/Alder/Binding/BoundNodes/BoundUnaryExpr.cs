using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundUnaryExpr(
    TokenType Operator,
    BoundExpr Operand,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal Type? PromotedType { get; init; }
    internal override BoundNodeKind Kind => BoundNodeKind.UnaryOperator;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Operand); }
}
