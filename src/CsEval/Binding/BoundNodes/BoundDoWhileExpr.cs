using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundDoWhileExpr(
    ImmutableArray<BoundExpr> Body,
    BoundExpr Condition,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.DoStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        foreach (var s in Body) visit(s);
        visit(Condition);
    }
}
