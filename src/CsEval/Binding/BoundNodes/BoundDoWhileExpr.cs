using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundDoWhileExpr(
    ImmutableArray<BoundExpr> Body,
    BoundExpr Condition,
    Type StaticType) : BoundExpr(StaticType);
