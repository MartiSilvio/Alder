using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundForExpr(
    ImmutableArray<BoundExpr> Initializers,
    BoundExpr? Condition,
    ImmutableArray<BoundExpr> Increments,
    ImmutableArray<BoundExpr> Body,
    Type StaticType) : BoundExpr(StaticType);
