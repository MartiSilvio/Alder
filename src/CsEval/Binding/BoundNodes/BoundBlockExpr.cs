using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundBlockExpr(
    ImmutableArray<BoundExpr> Statements,
    BoundExpr? ReturnExpr,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        foreach (var s in Statements) visit(s);
        if (ReturnExpr != null) visit(ReturnExpr);
    }
}
