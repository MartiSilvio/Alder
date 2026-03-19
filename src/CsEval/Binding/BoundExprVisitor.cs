using CsEval.Binding.BoundNodes;

namespace CsEval.Binding;

internal abstract class BoundExprVisitor<T>
{
    public T Visit(BoundExpr expr) => expr.Kind switch
    {
        BoundNodeKind.Literal => VisitLiteral((BoundLiteralExpr)expr),
        BoundNodeKind.Identifier => VisitIdentifier((BoundIdentifierExpr)expr),
        BoundNodeKind.InterpolatedString => VisitInterpolatedString((BoundInterpolatedStringExpr)expr),
        BoundNodeKind.Label => VisitLabel((BoundLabelExpr)expr),
        BoundNodeKind.UnaryOperator => VisitUnary((BoundUnaryExpr)expr),
        BoundNodeKind.BinaryOperator => VisitBinary((BoundBinaryExpr)expr),
        BoundNodeKind.LogicalOperator => VisitLogical((BoundLogicalExpr)expr),
        BoundNodeKind.ChainedComparisonOperator => VisitChainedComparison((BoundChainedComparisonExpr)expr),
        BoundNodeKind.IncrementOperator => VisitIncrementDecrement((BoundIncrementDecrementExpr)expr),
        BoundNodeKind.ConditionalOperator => VisitConditional((BoundConditionalExpr)expr),
        BoundNodeKind.NullCoalescingOperator => VisitNullCoalesce((BoundNullCoalesceExpr)expr),
        BoundNodeKind.IsPatternExpression => VisitIsPattern((BoundIsPatternExpr)expr),
        BoundNodeKind.AsOperator => VisitAs((BoundAsExpr)expr),
        BoundNodeKind.Conversion => VisitCast((BoundCastExpr)expr),
        BoundNodeKind.CheckedExpression => VisitChecked((BoundCheckedExpr)expr),
        BoundNodeKind.MemberAccess => VisitMemberAccess((BoundMemberAccessExpr)expr),
        BoundNodeKind.IndexerAccess => VisitIndexAccess((BoundIndexAccessExpr)expr),
        BoundNodeKind.MultiDimIndexAccess => VisitMultiDimIndexAccess((BoundMultiDimIndexAccessExpr)expr),
        BoundNodeKind.FromEndIndexExpression => VisitIndexFromEnd((BoundIndexFromEndExpr)expr),
        BoundNodeKind.RangeExpression => VisitRange((BoundRangeExpr)expr),
        BoundNodeKind.SliceExpression => VisitSlice((BoundSliceExpr)expr),
        BoundNodeKind.Call => VisitCall((BoundCallExpr)expr),
        BoundNodeKind.Invoke => VisitInvoke((BoundInvokeExpr)expr),
        BoundNodeKind.PipelineExpression => VisitPipeline((BoundPipelineExpr)expr),
        BoundNodeKind.NamedArgument => VisitNamedArgument((BoundNamedArgumentExpr)expr),
        BoundNodeKind.OutArgument => VisitOutArg((BoundOutArgExpr)expr),
        BoundNodeKind.Lambda => VisitLambda((BoundLambdaExpr)expr),
        BoundNodeKind.AssignmentOperator => VisitAssign((BoundAssignExpr)expr),
        BoundNodeKind.CompoundAssignmentOperator => VisitCompoundAssign((BoundCompoundAssignExpr)expr),
        BoundNodeKind.NullCoalescingAssignmentOperator => VisitNullCoalesceAssign((BoundNullCoalesceAssignExpr)expr),
        BoundNodeKind.MemberAssignment => VisitMemberAssign((BoundMemberAssignExpr)expr),
        BoundNodeKind.MemberCompoundAssignment => VisitMemberCompoundAssign((BoundMemberCompoundAssignExpr)expr),
        BoundNodeKind.MemberNullCoalesceAssignment => VisitMemberNullCoalesceAssign((BoundMemberNullCoalesceAssignExpr)expr),
        BoundNodeKind.MemberIncrement => VisitMemberIncrement((BoundMemberIncrementExpr)expr),
        BoundNodeKind.IndexAssignment => VisitIndexAssign((BoundIndexAssignExpr)expr),
        BoundNodeKind.IndexCompoundAssignment => VisitIndexCompoundAssign((BoundIndexCompoundAssignExpr)expr),
        BoundNodeKind.IndexNullCoalesceAssignment => VisitIndexNullCoalesceAssign((BoundIndexNullCoalesceAssignExpr)expr),
        BoundNodeKind.IndexIncrement => VisitIndexIncrement((BoundIndexIncrementExpr)expr),
        BoundNodeKind.MultiDimIndexAssignment => VisitMultiDimIndexAssign((BoundMultiDimIndexAssignExpr)expr),
        BoundNodeKind.DeconstructionAssignment => VisitDeconstruction((BoundDeconstructionExpr)expr),
        BoundNodeKind.ObjectCreationExpression => VisitObjectCreation((BoundObjectCreationExpr)expr),
        BoundNodeKind.ObjectLiteral => VisitObjectLiteral((BoundObjectLiteralExpr)expr),
        BoundNodeKind.ArrayLiteral => VisitArrayLiteral((BoundArrayLiteralExpr)expr),
        BoundNodeKind.TypedArrayCreation => VisitTypedArrayCreation((BoundTypedArrayCreationExpr)expr),
        BoundNodeKind.TypedArrayLiteral => VisitTypedArrayLiteral((BoundTypedArrayLiteralExpr)expr),
        BoundNodeKind.MultiDimArrayInit => VisitMultiDimArrayInit((BoundMultiDimArrayInitExpr)expr),
        BoundNodeKind.MultiDimTypedArrayCreation => VisitMultiDimTypedArrayCreation((BoundMultiDimTypedArrayCreationExpr)expr),
        BoundNodeKind.TupleLiteral => VisitTuple((BoundTupleExpr)expr),
        BoundNodeKind.SpreadElement => VisitSpread((BoundSpreadExpr)expr),
        BoundNodeKind.VariableDeclaration => VisitVariableDecl((BoundVariableDeclExpr)expr),
        BoundNodeKind.Block => VisitBlock((BoundBlockExpr)expr),
        BoundNodeKind.ReturnStatement => VisitReturn((BoundReturnExpr)expr),
        BoundNodeKind.BreakStatement => VisitBreak((BoundBreakExpr)expr),
        BoundNodeKind.ContinueStatement => VisitContinue((BoundContinueExpr)expr),
        BoundNodeKind.IfStatement => VisitIfStatement((BoundIfStatementExpr)expr),
        BoundNodeKind.WhileStatement => VisitWhile((BoundWhileExpr)expr),
        BoundNodeKind.DoStatement => VisitDoWhile((BoundDoWhileExpr)expr),
        BoundNodeKind.ForStatement => VisitFor((BoundForExpr)expr),
        BoundNodeKind.ForEachStatement => VisitForEach((BoundForEachExpr)expr),
        BoundNodeKind.SwitchStatement => VisitSwitchStatement((BoundSwitchStatementExpr)expr),
        BoundNodeKind.SwitchExpression => VisitSwitchExpression((BoundSwitchExpressionExpr)expr),
        BoundNodeKind.UsingStatement => VisitUsingStatement((BoundUsingStatementExpr)expr),
        BoundNodeKind.LockStatement => VisitLockStatement((BoundLockStatementExpr)expr),
        BoundNodeKind.GotoStatement => VisitGoto((BoundGotoExpr)expr),
        BoundNodeKind.GotoCaseStatement => VisitGotoCase((BoundGotoCaseExpr)expr),
        BoundNodeKind.GotoDefaultStatement => VisitGotoDefault((BoundGotoDefaultExpr)expr),
        BoundNodeKind.ThrowExpression => VisitThrow((BoundThrowExpr)expr),
        BoundNodeKind.ThrowStatement => VisitThrowStatement((BoundThrowStatementExpr)expr),
        BoundNodeKind.TryStatement => VisitTryCatchFinally((BoundTryCatchFinallyExpr)expr),
        _ => DefaultVisit(expr)
    };

    protected virtual T DefaultVisit(BoundExpr node) => default!;

    protected virtual T VisitLiteral(BoundLiteralExpr node) => DefaultVisit(node);
    protected virtual T VisitIdentifier(BoundIdentifierExpr node) => DefaultVisit(node);
    protected virtual T VisitInterpolatedString(BoundInterpolatedStringExpr node) => DefaultVisit(node);
    protected virtual T VisitLabel(BoundLabelExpr node) => DefaultVisit(node);
    protected virtual T VisitUnary(BoundUnaryExpr node) => DefaultVisit(node);
    protected virtual T VisitBinary(BoundBinaryExpr node) => DefaultVisit(node);
    protected virtual T VisitLogical(BoundLogicalExpr node) => DefaultVisit(node);
    protected virtual T VisitChainedComparison(BoundChainedComparisonExpr node) => DefaultVisit(node);
    protected virtual T VisitIncrementDecrement(BoundIncrementDecrementExpr node) => DefaultVisit(node);
    protected virtual T VisitConditional(BoundConditionalExpr node) => DefaultVisit(node);
    protected virtual T VisitNullCoalesce(BoundNullCoalesceExpr node) => DefaultVisit(node);
    protected virtual T VisitIsPattern(BoundIsPatternExpr node) => DefaultVisit(node);
    protected virtual T VisitAs(BoundAsExpr node) => DefaultVisit(node);
    protected virtual T VisitCast(BoundCastExpr node) => DefaultVisit(node);
    protected virtual T VisitChecked(BoundCheckedExpr node) => DefaultVisit(node);
    protected virtual T VisitMemberAccess(BoundMemberAccessExpr node) => DefaultVisit(node);
    protected virtual T VisitIndexAccess(BoundIndexAccessExpr node) => DefaultVisit(node);
    protected virtual T VisitMultiDimIndexAccess(BoundMultiDimIndexAccessExpr node) => DefaultVisit(node);
    protected virtual T VisitIndexFromEnd(BoundIndexFromEndExpr node) => DefaultVisit(node);
    protected virtual T VisitRange(BoundRangeExpr node) => DefaultVisit(node);
    protected virtual T VisitSlice(BoundSliceExpr node) => DefaultVisit(node);
    protected virtual T VisitCall(BoundCallExpr node) => DefaultVisit(node);
    protected virtual T VisitInvoke(BoundInvokeExpr node) => DefaultVisit(node);
    protected virtual T VisitPipeline(BoundPipelineExpr node) => DefaultVisit(node);
    protected virtual T VisitNamedArgument(BoundNamedArgumentExpr node) => DefaultVisit(node);
    protected virtual T VisitOutArg(BoundOutArgExpr node) => DefaultVisit(node);
    protected virtual T VisitLambda(BoundLambdaExpr node) => DefaultVisit(node);
    protected virtual T VisitAssign(BoundAssignExpr node) => DefaultVisit(node);
    protected virtual T VisitCompoundAssign(BoundCompoundAssignExpr node) => DefaultVisit(node);
    protected virtual T VisitNullCoalesceAssign(BoundNullCoalesceAssignExpr node) => DefaultVisit(node);
    protected virtual T VisitMemberAssign(BoundMemberAssignExpr node) => DefaultVisit(node);
    protected virtual T VisitMemberCompoundAssign(BoundMemberCompoundAssignExpr node) => DefaultVisit(node);
    protected virtual T VisitMemberNullCoalesceAssign(BoundMemberNullCoalesceAssignExpr node) => DefaultVisit(node);
    protected virtual T VisitMemberIncrement(BoundMemberIncrementExpr node) => DefaultVisit(node);
    protected virtual T VisitIndexAssign(BoundIndexAssignExpr node) => DefaultVisit(node);
    protected virtual T VisitIndexCompoundAssign(BoundIndexCompoundAssignExpr node) => DefaultVisit(node);
    protected virtual T VisitIndexNullCoalesceAssign(BoundIndexNullCoalesceAssignExpr node) => DefaultVisit(node);
    protected virtual T VisitIndexIncrement(BoundIndexIncrementExpr node) => DefaultVisit(node);
    protected virtual T VisitMultiDimIndexAssign(BoundMultiDimIndexAssignExpr node) => DefaultVisit(node);
    protected virtual T VisitDeconstruction(BoundDeconstructionExpr node) => DefaultVisit(node);
    protected virtual T VisitObjectCreation(BoundObjectCreationExpr node) => DefaultVisit(node);
    protected virtual T VisitObjectLiteral(BoundObjectLiteralExpr node) => DefaultVisit(node);
    protected virtual T VisitArrayLiteral(BoundArrayLiteralExpr node) => DefaultVisit(node);
    protected virtual T VisitTypedArrayCreation(BoundTypedArrayCreationExpr node) => DefaultVisit(node);
    protected virtual T VisitTypedArrayLiteral(BoundTypedArrayLiteralExpr node) => DefaultVisit(node);
    protected virtual T VisitMultiDimArrayInit(BoundMultiDimArrayInitExpr node) => DefaultVisit(node);
    protected virtual T VisitMultiDimTypedArrayCreation(BoundMultiDimTypedArrayCreationExpr node) => DefaultVisit(node);
    protected virtual T VisitTuple(BoundTupleExpr node) => DefaultVisit(node);
    protected virtual T VisitSpread(BoundSpreadExpr node) => DefaultVisit(node);
    protected virtual T VisitVariableDecl(BoundVariableDeclExpr node) => DefaultVisit(node);
    protected virtual T VisitBlock(BoundBlockExpr node) => DefaultVisit(node);
    protected virtual T VisitReturn(BoundReturnExpr node) => DefaultVisit(node);
    protected virtual T VisitBreak(BoundBreakExpr node) => DefaultVisit(node);
    protected virtual T VisitContinue(BoundContinueExpr node) => DefaultVisit(node);
    protected virtual T VisitIfStatement(BoundIfStatementExpr node) => DefaultVisit(node);
    protected virtual T VisitWhile(BoundWhileExpr node) => DefaultVisit(node);
    protected virtual T VisitDoWhile(BoundDoWhileExpr node) => DefaultVisit(node);
    protected virtual T VisitFor(BoundForExpr node) => DefaultVisit(node);
    protected virtual T VisitForEach(BoundForEachExpr node) => DefaultVisit(node);
    protected virtual T VisitSwitchStatement(BoundSwitchStatementExpr node) => DefaultVisit(node);
    protected virtual T VisitSwitchExpression(BoundSwitchExpressionExpr node) => DefaultVisit(node);
    protected virtual T VisitUsingStatement(BoundUsingStatementExpr node) => DefaultVisit(node);
    protected virtual T VisitLockStatement(BoundLockStatementExpr node) => DefaultVisit(node);
    protected virtual T VisitGoto(BoundGotoExpr node) => DefaultVisit(node);
    protected virtual T VisitGotoCase(BoundGotoCaseExpr node) => DefaultVisit(node);
    protected virtual T VisitGotoDefault(BoundGotoDefaultExpr node) => DefaultVisit(node);
    protected virtual T VisitThrow(BoundThrowExpr node) => DefaultVisit(node);
    protected virtual T VisitThrowStatement(BoundThrowStatementExpr node) => DefaultVisit(node);
    protected virtual T VisitTryCatchFinally(BoundTryCatchFinallyExpr node) => DefaultVisit(node);
}

internal abstract class BoundExprWalker : BoundExprVisitor<object?>
{
    protected override object? DefaultVisit(BoundExpr node)
    {
        node.EnumerateChildren(child => Visit(child));
        return null;
    }
}
