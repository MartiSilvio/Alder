using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.IfStatement, "IfStatement")]
internal sealed partial record BoundIfStatementExpr(
    BoundExpr Condition,
    ImmutableArray<BoundExpr> ThenStatements,
    ImmutableArray<BoundExpr> ElseStatements,
    BoundType StaticType) : BoundExpr(StaticType);
