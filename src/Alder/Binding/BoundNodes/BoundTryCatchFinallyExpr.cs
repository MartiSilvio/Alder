using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundContainer]
internal sealed partial record BoundCatchClause(
    string? ExceptionTypeName,
    string? VariableName,
    BoundExpr? WhenGuard,
    ImmutableArray<BoundExpr> Body,
    int? LocalId = null);

[BoundNode(BoundNodeKind.TryStatement, "TryCatchFinally")]
internal sealed partial record BoundTryCatchFinallyExpr(
    ImmutableArray<BoundExpr> TryBody,
    ImmutableArray<BoundCatchClause> CatchClauses,
    ImmutableArray<BoundExpr> FinallyBody,
    BoundType StaticType) : BoundExpr(StaticType);
