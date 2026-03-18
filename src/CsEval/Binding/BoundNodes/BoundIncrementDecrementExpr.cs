using CsEval.Parsing;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIncrementDecrementExpr(
    string Name,
    TokenType Operator,
    bool IsPrefix,
    Type StaticType,
    int? LocalId = null) : BoundExpr(StaticType);
