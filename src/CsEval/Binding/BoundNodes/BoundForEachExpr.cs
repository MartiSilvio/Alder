using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundForEachExpr(
    string VariableName,
    BoundExpr Collection,
    ImmutableArray<BoundExpr> Body,
    Type StaticType) : BoundExpr(StaticType);
