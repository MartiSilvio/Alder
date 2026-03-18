using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMultiDimIndexAccessExpr(
    BoundExpr Target,
    ImmutableArray<BoundExpr> Indices,
    bool NullSafe,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Target);
        foreach (var i in Indices) visit(i);
    }
}
