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
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        foreach (var s in TryBody) visit(s);
        foreach (var c in CatchClauses)
        {
            if (c.WhenGuard != null) visit(c.WhenGuard);
            foreach (var s in c.Body) visit(s);
        }
        foreach (var s in FinallyBody) visit(s);
    }
}
