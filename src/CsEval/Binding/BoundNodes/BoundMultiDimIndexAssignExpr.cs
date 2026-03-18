using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMultiDimIndexAssignExpr(
    BoundExpr Target,
    ImmutableArray<BoundExpr> Indices,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Target);
        foreach (var i in Indices) visit(i);
        visit(Value);
    }
}
