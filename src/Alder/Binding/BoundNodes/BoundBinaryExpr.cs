using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundBinaryExpr(
    TokenType Operator,
    BoundExpr Left,
    BoundExpr Right,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal Type? PromotedType { get; init; }
    internal override BoundNodeKind Kind => BoundNodeKind.BinaryOperator;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Left); visit(Right); }
}
