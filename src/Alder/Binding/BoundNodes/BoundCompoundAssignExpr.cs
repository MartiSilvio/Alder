using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.CompoundAssignmentOperator, "CompoundAssign")]
internal sealed partial record BoundCompoundAssignExpr(
    string Name,
    TokenType Operator,
    BoundExpr Value,
    BoundType StaticType,
    int? LocalId = null) : BoundExpr(StaticType);
