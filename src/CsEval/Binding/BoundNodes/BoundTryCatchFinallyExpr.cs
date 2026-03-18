using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundCatchClause(
    string? ExceptionTypeName,
    string? VariableName,
    BoundExpr? WhenGuard,
    ImmutableArray<BoundExpr> Body,
    int? LocalId = null);

internal sealed record BoundTryCatchFinallyExpr(
    ImmutableArray<BoundExpr> TryBody,
    ImmutableArray<BoundCatchClause> CatchClauses,
    ImmutableArray<BoundExpr> FinallyBody,
    Type StaticType) : BoundExpr(StaticType);
