using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMultiDimIndexAccessExpr(
    BoundExpr Target,
    ImmutableArray<BoundExpr> Indices,
    bool NullSafe,
    Type StaticType) : BoundExpr(StaticType);
