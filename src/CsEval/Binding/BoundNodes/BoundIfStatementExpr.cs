using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIfStatementExpr(
    BoundExpr Condition,
    ImmutableArray<BoundExpr> ThenStatements,
    ImmutableArray<BoundExpr> ElseStatements,
    Type StaticType) : BoundExpr(StaticType);
