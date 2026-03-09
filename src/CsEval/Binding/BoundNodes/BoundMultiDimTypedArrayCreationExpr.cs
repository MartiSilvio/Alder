using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundMultiDimTypedArrayCreationExpr(
    string ElementTypeName,
    ImmutableArray<BoundExpr> Sizes,
    Type StaticType) : BoundExpr(StaticType);
