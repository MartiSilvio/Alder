using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMultiDimTypedArrayCreationExpr(
    string ElementTypeName,
    ImmutableArray<BoundExpr> Sizes,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { foreach (var s in Sizes) visit(s); }
}
