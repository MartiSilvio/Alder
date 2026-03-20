using CsEval.Binding.BoundNodes;

namespace CsEval.Binding.Optimization;

internal sealed class DeadBranchEliminationPass : BoundExprRewriter
{
    protected override BoundExpr VisitConditional(BoundConditionalExpr node)
    {
        var rewritten = (BoundConditionalExpr)base.VisitConditional(node);
        if (rewritten.Condition is not BoundLiteralExpr { Value: bool condVal })
            return rewritten;

        var chosen = condVal ? rewritten.ThenBranch : rewritten.ElseBranch;

        if (rewritten.ThenBranch.StaticType != rewritten.ElseBranch.StaticType &&
            rewritten.StaticType != typeof(object) &&
            chosen.StaticType != rewritten.StaticType)
        {
            var cast = new BoundCastExpr(chosen, rewritten.StaticType, chosen.StaticType, rewritten.StaticType);
            cast.Span = rewritten.Span;
            return cast;
        }

        return chosen;
    }

    protected override BoundExpr VisitIfStatement(BoundIfStatementExpr node)
    {
        var rewritten = (BoundIfStatementExpr)base.VisitIfStatement(node);
        if (rewritten.Condition is not BoundLiteralExpr { Value: bool condVal })
            return rewritten;

        if (condVal)
        {
            var block = new BoundBlockExpr(rewritten.ThenStatements, null, rewritten.StaticType);
            block.Span = rewritten.Span;
            return block;
        }

        if (!rewritten.ElseStatements.IsEmpty)
        {
            var block = new BoundBlockExpr(rewritten.ElseStatements, null, rewritten.StaticType);
            block.Span = rewritten.Span;
            return block;
        }

        // false with no else → no-op literal
        var noop = new BoundLiteralExpr(null, typeof(object));
        noop.Span = rewritten.Span;
        return noop;
    }
}
