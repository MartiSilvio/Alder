using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundWhileExpr(
    BoundExpr Condition,
    ImmutableArray<BoundExpr> Body,
    Type StaticType) : BoundExpr(StaticType);
