using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundTupleExpr(
    ImmutableArray<BoundExpr> Elements,
    ImmutableArray<string?> ElementNames,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { foreach (var e in Elements) visit(e); }
}
