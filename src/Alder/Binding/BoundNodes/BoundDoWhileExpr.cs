using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundDoWhileExpr(
    ImmutableArray<BoundExpr> Body,
    BoundExpr Condition,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.DoStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        foreach (var s in Body) visit(s);
        visit(Condition);
    }
}
