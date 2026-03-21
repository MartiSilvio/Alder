using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundWhileExpr(
    BoundExpr Condition,
    ImmutableArray<BoundExpr> Body,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.WhileStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Condition);
        foreach (var s in Body) visit(s);
    }
}
