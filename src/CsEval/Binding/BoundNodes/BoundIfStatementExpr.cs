using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundIfStatementExpr(
    BoundExpr Condition,
    ImmutableArray<BoundExpr> ThenStatements,
    ImmutableArray<BoundExpr> ElseStatements,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Condition);
        foreach (var s in ThenStatements) visit(s);
        foreach (var s in ElseStatements) visit(s);
    }
}
