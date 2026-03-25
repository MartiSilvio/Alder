using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundArrayLiteralExpr(
    ImmutableArray<BoundExpr> Elements,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ArrayLiteral;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { foreach (var e in Elements) visit(e); }
}
