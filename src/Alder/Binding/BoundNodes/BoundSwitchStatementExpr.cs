using System.Collections.Immutable;
using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundContainer]
internal sealed partial record BoundSwitchCase(
    Pattern? CasePattern,
    BoundExpr? WhenGuard,
    ImmutableArray<BoundExpr> Statements);

[BoundNode(BoundNodeKind.SwitchStatement, "SwitchStatement")]
internal sealed partial record BoundSwitchStatementExpr(
    BoundExpr Expression,
    ImmutableArray<BoundSwitchCase> Cases,
    BoundType StaticType) : BoundExpr(StaticType);
