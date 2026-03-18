using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMultiDimArrayInitExpr(
    string ElementTypeName,
    int Rank,
    ImmutableArray<BoundExpr>? ExplicitSizes,
    ImmutableArray<BoundExpr> FlatValues,
    int[] InferredDimensions,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        if (ExplicitSizes != null)
            foreach (var s in ExplicitSizes.Value) visit(s);
        foreach (var v in FlatValues) visit(v);
    }
}
