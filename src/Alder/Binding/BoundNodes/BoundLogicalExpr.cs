using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.LogicalOperator, "Logical", ChainFlatten = true, HasRevisitHook = true)]
internal sealed partial record BoundLogicalExpr(
    TokenType Operator,
    BoundExpr Left,
    BoundExpr Right,
    BoundType StaticType) : BoundExpr(StaticType);
