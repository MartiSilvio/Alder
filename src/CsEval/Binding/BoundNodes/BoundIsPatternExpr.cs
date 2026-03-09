using CsEval.Parsing;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIsPatternExpr(
    BoundExpr Expression,
    Pattern Pattern,
    Type StaticType) : BoundExpr(StaticType);
