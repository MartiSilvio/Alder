using CsEval.Parsing;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundLogicalExpr(
    TokenType Operator,
    BoundExpr Left,
    BoundExpr Right,
    Type StaticType) : BoundExpr(StaticType);
