using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundTupleExpr(
    ImmutableArray<BoundExpr> Elements,
    Type StaticType) : BoundExpr(StaticType);
