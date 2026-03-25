using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundTupleExpr(
    ImmutableArray<BoundExpr> Elements,
    ImmutableArray<string?> ElementNames,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.TupleLiteral;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { foreach (var e in Elements) visit(e); }
}
