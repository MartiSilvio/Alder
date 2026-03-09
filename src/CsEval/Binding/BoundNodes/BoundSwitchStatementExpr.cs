using CsEval.Parsing;
using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundSwitchCase(
    Pattern? CasePattern,
    BoundExpr? WhenGuard,
    ImmutableArray<BoundExpr> Statements);

internal sealed record BoundSwitchStatementExpr(
    BoundExpr Expression,
    ImmutableArray<BoundSwitchCase> Cases,
    Type StaticType) : BoundExpr(StaticType);
