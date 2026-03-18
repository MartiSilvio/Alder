using CsEval.Parsing;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundCompoundAssignExpr(
    string Name,
    TokenType Operator,
    BoundExpr Value,
    Type StaticType,
    int? LocalId = null) : BoundExpr(StaticType);
