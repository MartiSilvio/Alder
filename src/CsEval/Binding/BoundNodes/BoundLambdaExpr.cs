using System.Collections.Immutable;
using CsEval.Parsing;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundLambdaExpr(
    ImmutableArray<string> Parameters,
    Expr Body,
    Type StaticType) : BoundExpr(StaticType);
