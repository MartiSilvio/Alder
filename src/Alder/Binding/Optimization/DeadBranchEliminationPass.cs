using Alder.Binding.BoundNodes;

namespace Alder.Binding.Optimization;

internal sealed class DeadBranchEliminationPass : BoundExprRewriter
{
    protected override BoundExpr VisitIfStatement(BoundIfStatementExpr node)
    {
        var rewritten = (BoundIfStatementExpr)base.VisitIfStatement(node);
        if (rewritten.Condition is not BoundLiteralExpr { Value: bool condVal })
            return rewritten;

        if (condVal)
        {
            var block = new BoundBlockExpr(rewritten.ThenStatements, null, rewritten.StaticType)
            {
                Span = rewritten.Span
            };
            return block;
        }

        if (!rewritten.ElseStatements.IsEmpty)
        {
            var block = new BoundBlockExpr(rewritten.ElseStatements, null, rewritten.StaticType)
            {
                Span = rewritten.Span
            };
            return block;
        }

        // false with no else → no-op literal
        var noop = new BoundLiteralExpr(null, BoundType.Void)
        {
            Span = rewritten.Span
        };
        return noop;
    }
}
