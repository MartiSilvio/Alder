using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.BinaryOperator, "Binary", ChainFlatten = true, HasRevisitHook = true)]
internal sealed partial record BoundBinaryExpr(
    TokenType Operator,
    BoundExpr Left,
    BoundExpr Right,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal Type? PromotedType { get; init; }
}
