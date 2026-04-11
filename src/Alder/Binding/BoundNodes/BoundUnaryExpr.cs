using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.UnaryOperator, "Unary")]
internal sealed partial record BoundUnaryExpr(
    TokenType Operator,
    BoundExpr Operand,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal Type? PromotedType { get; init; }
}
