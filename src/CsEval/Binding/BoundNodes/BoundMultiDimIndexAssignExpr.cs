using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMultiDimIndexAssignExpr(
    BoundExpr Target,
    ImmutableArray<BoundExpr> Indices,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType);
