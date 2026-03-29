using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundMultiDimArrayInitExpr(
    Type ElementType,
    int Rank,
    ImmutableArray<BoundExpr>? ExplicitSizes,
    ImmutableArray<BoundExpr> FlatValues,
    int[] InferredDimensions,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.MultiDimArrayInit;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        if (ExplicitSizes != null)
            foreach (var s in ExplicitSizes.Value) visit(s);
        foreach (var v in FlatValues) visit(v);
    }
}
