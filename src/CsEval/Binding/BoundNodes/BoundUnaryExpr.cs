using CsEval.Parsing;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundUnaryExpr(
    TokenType Operator,
    BoundExpr Operand,
    Type StaticType) : BoundExpr(StaticType);
