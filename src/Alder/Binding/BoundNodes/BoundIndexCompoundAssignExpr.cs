using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.IndexCompoundAssignment, "IndexCompoundAssign")]
internal sealed partial record BoundIndexCompoundAssignExpr(
    BoundExpr Target,
    BoundExpr Index,
    TokenType Operator,
    BoundExpr Value,
    BoundType StaticType) : BoundExpr(StaticType);
