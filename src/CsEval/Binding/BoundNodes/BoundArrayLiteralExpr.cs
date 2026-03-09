using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundArrayLiteralExpr(
    ImmutableArray<BoundExpr> Elements,
    Type StaticType) : BoundExpr(StaticType);
