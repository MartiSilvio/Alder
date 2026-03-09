using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundBlockExpr(
    ImmutableArray<BoundExpr> Statements,
    BoundExpr? ReturnExpr,
    Type StaticType) : BoundExpr(StaticType);
