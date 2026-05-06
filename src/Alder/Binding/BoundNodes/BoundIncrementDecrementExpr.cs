using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.IncrementOperator, "IncrementDecrement")]
internal sealed partial record BoundIncrementDecrementExpr(
    string Name,
    TokenType Operator,
    bool IsPrefix,
    BoundType StaticType,
    int? LocalId = null) : BoundExpr(StaticType);
