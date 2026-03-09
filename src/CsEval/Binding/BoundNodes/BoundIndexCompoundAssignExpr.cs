using CsEval.Parsing;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIndexCompoundAssignExpr(
    BoundExpr Target,
    BoundExpr Index,
    TokenType Operator,
    BoundExpr Value,
    Type StaticType) : BoundExpr(StaticType);
