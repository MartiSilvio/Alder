using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundTypedArrayLiteralExpr(
    string ElementTypeName,
    ImmutableArray<BoundExpr> Elements,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { foreach (var e in Elements) visit(e); }
}
