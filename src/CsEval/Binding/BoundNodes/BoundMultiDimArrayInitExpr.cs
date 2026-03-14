using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMultiDimArrayInitExpr(
    string ElementTypeName,
    int Rank,
    ImmutableArray<BoundExpr>? ExplicitSizes,
    ImmutableArray<BoundExpr> FlatValues,
    int[] InferredDimensions,
    Type StaticType) : BoundExpr(StaticType);
