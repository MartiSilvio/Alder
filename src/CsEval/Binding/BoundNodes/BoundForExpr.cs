using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundForExpr(
    ImmutableArray<BoundExpr> Initializers,
    BoundExpr? Condition,
    ImmutableArray<BoundExpr> Increments,
    ImmutableArray<BoundExpr> Body,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        foreach (var i in Initializers) visit(i);
        if (Condition != null) visit(Condition);
        foreach (var i in Increments) visit(i);
        foreach (var s in Body) visit(s);
    }
}
