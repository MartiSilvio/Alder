using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.MultiDimArrayInit, "MultiDimArrayInit")]
internal sealed partial record BoundMultiDimArrayInitExpr(
    Type ElementType,
    int Rank,
    ImmutableArray<BoundExpr>? ExplicitSizes,
    ImmutableArray<BoundExpr> FlatValues,
    int[] InferredDimensions,
    BoundType StaticType) : BoundExpr(StaticType);
