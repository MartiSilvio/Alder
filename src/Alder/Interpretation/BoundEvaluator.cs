using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Runtime.Semantics;
using Alder.Text;
using Alder.Tracing;

namespace Alder.Interpretation;

internal sealed partial class BoundEvaluator
{
    private AlderContext _context;
    private readonly AlderOptions _options;
    private readonly CancellationToken _cancellationToken;
    private readonly List<EvaluationTraceStep>? _traceSteps;
    private readonly Stack<Exception> _caughtExceptions = new();
    private readonly SourceText? _sourceText;
    private int _breakContextDepth;
    private int _loopDepth;
    private bool _isChecked;

    public BoundEvaluator(
        AlderContext context,
        AlderOptions options,
        CancellationToken cancellationToken = default,
        List<EvaluationTraceStep>? traceSteps = null,
        SourceText? sourceText = null)
    {
        _context = context;
        _options = options;
        _cancellationToken = cancellationToken;
        _traceSteps = traceSteps;
        _sourceText = sourceText;
    }

    public object? Evaluate(BoundExpr expr)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        object? result;
        try
        {
            result = EvaluateCore(expr);
        }
        catch (AlderException ex) when (ex.Span.IsEmpty && !expr.Span.IsEmpty)
        {
            int? line = null, column = null;
            if (_sourceText != null)
            {
                var pos = _sourceText.GetLinePosition(expr.Span.Start);
                line = pos.Line + 1;
                column = pos.Character + 1;
            }
            ex.EnrichDiagnosticsWithPosition(expr.Span, line, column);
            throw;
        }

        RecordTrace(expr, result);
        return result;
    }

    private object? EvaluateCore(BoundExpr expr)
    {
        if (expr.HasErrors)
        {
            var diag = expr.Diagnostic;
            if (diag != null)
            {
                var ex = new AlderException(DiagnosticDescriptors.BindingFailed, diag.Message);
                ex.SetDiagnostics([diag]);
                throw ex;
            }
            throw new AlderException(DiagnosticDescriptors.BindingFailed, "Expression has errors");
        }

        return expr.Kind switch
        {
            BoundNodeKind.Literal => ((BoundLiteralExpr)expr).Value,
            BoundNodeKind.Identifier => IdentifierRuntime.ResolveIdentifier(((BoundIdentifierExpr)expr).Name, _context, _options),
            BoundNodeKind.Conversion => EvaluateCast((BoundCastExpr)expr),
            BoundNodeKind.AsOperator => EvaluateAs((BoundAsExpr)expr),
            BoundNodeKind.IsPatternExpression => EvaluateIsPattern((BoundIsPatternExpr)expr),
            BoundNodeKind.ArrayLiteral => EvaluateArrayLiteral((BoundArrayLiteralExpr)expr),
            BoundNodeKind.ObjectLiteral => EvaluateObjectLiteral((BoundObjectLiteralExpr)expr),
            BoundNodeKind.SpreadElement => EvaluateSpread((BoundSpreadExpr)expr),
            BoundNodeKind.SliceExpression => EvaluateSlice((BoundSliceExpr)expr),
            BoundNodeKind.ObjectCreationExpression => EvaluateObjectCreation((BoundObjectCreationExpr)expr),
            BoundNodeKind.TypedArrayCreation => EvaluateTypedArrayCreation((BoundTypedArrayCreationExpr)expr),
            BoundNodeKind.TypedArrayLiteral => EvaluateTypedArrayLiteral((BoundTypedArrayLiteralExpr)expr),
            BoundNodeKind.MultiDimTypedArrayCreation => EvaluateMultiDimTypedArrayCreation((BoundMultiDimTypedArrayCreationExpr)expr),
            BoundNodeKind.MultiDimArrayInit => EvaluateMultiDimArrayInit((BoundMultiDimArrayInitExpr)expr),
            BoundNodeKind.TupleLiteral => EvaluateTuple((BoundTupleExpr)expr),
            BoundNodeKind.DeconstructionAssignment => EvaluateDeconstruction((BoundDeconstructionExpr)expr),
            BoundNodeKind.InterpolatedString => EvaluateInterpolatedString((BoundInterpolatedStringExpr)expr),
            BoundNodeKind.UnaryOperator => EvaluateUnary((BoundUnaryExpr)expr),
            BoundNodeKind.BinaryOperator => EvaluateBinary((BoundBinaryExpr)expr),
            BoundNodeKind.LogicalOperator => EvaluateLogical((BoundLogicalExpr)expr),
            BoundNodeKind.NullCoalescingOperator => EvaluateNullCoalesce((BoundNullCoalesceExpr)expr),
            BoundNodeKind.ConditionalOperator => EvaluateConditional((BoundConditionalExpr)expr),
            BoundNodeKind.Block => EvaluateBlock((BoundBlockExpr)expr),
            BoundNodeKind.IfStatement => EvaluateIfStatement((BoundIfStatementExpr)expr),
            BoundNodeKind.WhileStatement => EvaluateWhile((BoundWhileExpr)expr),
            BoundNodeKind.ForStatement => EvaluateFor((BoundForExpr)expr),
            BoundNodeKind.DoStatement => EvaluateDoWhile((BoundDoWhileExpr)expr),
            BoundNodeKind.ForEachStatement => EvaluateForEach((BoundForEachExpr)expr),
            BoundNodeKind.UsingStatement => EvaluateUsingStatement((BoundUsingStatementExpr)expr),
            BoundNodeKind.LockStatement => EvaluateLockStatement((BoundLockStatementExpr)expr),
            BoundNodeKind.SwitchStatement => EvaluateSwitch((BoundSwitchStatementExpr)expr),
            BoundNodeKind.SwitchExpression => EvaluateSwitchExpression((BoundSwitchExpressionExpr)expr),
            BoundNodeKind.CheckedExpression => EvaluateChecked((BoundCheckedExpr)expr),
            BoundNodeKind.ChainedComparisonOperator => EvaluateChainedComparison((BoundChainedComparisonExpr)expr),
            BoundNodeKind.BreakStatement => EvaluateBreak(),
            BoundNodeKind.ContinueStatement => EvaluateContinue(),
            BoundNodeKind.GotoStatement => ControlFlowSignal.GotoSignal(((BoundGotoExpr)expr).Label),
            BoundNodeKind.GotoCaseStatement => ControlFlowSignal.GotoCaseSignal(Evaluate(((BoundGotoCaseExpr)expr).Value)),
            BoundNodeKind.GotoDefaultStatement => ControlFlowSignal.GotoDefaultSignal,
            BoundNodeKind.Label => null,
            BoundNodeKind.VariableDeclaration => EvaluateVariableDecl((BoundVariableDeclExpr)expr),
            BoundNodeKind.AssignmentOperator => EvaluateAssign((BoundAssignExpr)expr),
            BoundNodeKind.NullCoalescingAssignmentOperator => EvaluateNullCoalesceAssign((BoundNullCoalesceAssignExpr)expr),
            BoundNodeKind.CompoundAssignmentOperator => EvaluateCompoundAssign((BoundCompoundAssignExpr)expr),
            BoundNodeKind.IncrementOperator => EvaluateIncrementDecrement((BoundIncrementDecrementExpr)expr),
            BoundNodeKind.MemberAssignment => EvaluateMemberAssign((BoundMemberAssignExpr)expr),
            BoundNodeKind.IndexAssignment => EvaluateIndexAssign((BoundIndexAssignExpr)expr),
            BoundNodeKind.MemberCompoundAssignment => EvaluateMemberCompoundAssign((BoundMemberCompoundAssignExpr)expr),
            BoundNodeKind.IndexCompoundAssignment => EvaluateIndexCompoundAssign((BoundIndexCompoundAssignExpr)expr),
            BoundNodeKind.MemberNullCoalesceAssignment => EvaluateMemberNullCoalesceAssign((BoundMemberNullCoalesceAssignExpr)expr),
            BoundNodeKind.IndexNullCoalesceAssignment => EvaluateIndexNullCoalesceAssign((BoundIndexNullCoalesceAssignExpr)expr),
            BoundNodeKind.MemberIncrement => EvaluateMemberIncrement((BoundMemberIncrementExpr)expr),
            BoundNodeKind.IndexIncrement => EvaluateIndexIncrement((BoundIndexIncrementExpr)expr),
            BoundNodeKind.ThrowExpression => EvaluateThrow((BoundThrowExpr)expr),
            BoundNodeKind.TryStatement => EvaluateTryCatchFinally((BoundTryCatchFinallyExpr)expr),
            BoundNodeKind.ThrowStatement => EvaluateThrowStatement(),
            BoundNodeKind.ReturnStatement => EvaluateReturn((BoundReturnExpr)expr),
            BoundNodeKind.MemberAccess => EvaluateMemberAccess((BoundMemberAccessExpr)expr),
            BoundNodeKind.IndexerAccess => EvaluateIndexAccess((BoundIndexAccessExpr)expr),
            BoundNodeKind.MultiDimIndexAccess => EvaluateMultiDimIndexAccess((BoundMultiDimIndexAccessExpr)expr),
            BoundNodeKind.MultiDimIndexAssignment => EvaluateMultiDimIndexAssign((BoundMultiDimIndexAssignExpr)expr),
            BoundNodeKind.NamedArgument => EvaluateNamedArgument((BoundNamedArgumentExpr)expr),
            BoundNodeKind.OutArgument => EvaluateOutArg((BoundOutArgExpr)expr),
            BoundNodeKind.Call => EvaluateCall((BoundCallExpr)expr),
            BoundNodeKind.Invoke => EvaluateInvoke((BoundInvokeExpr)expr),
            BoundNodeKind.Lambda => EvaluateLambda((BoundLambdaExpr)expr),
            BoundNodeKind.PipelineExpression => EvaluatePipeline((BoundPipelineExpr)expr),
            BoundNodeKind.RangeExpression => EvaluateRange((BoundRangeExpr)expr),
            BoundNodeKind.FromEndIndexExpression => new Index(Convert.ToInt32(Evaluate(((BoundIndexFromEndExpr)expr).Operand)), fromEnd: true),
            _ => throw new BindingNotSupportedException(
                $"Bound execution for node '{expr.GetType().Name}' is not implemented")
        };
    }

    private void RecordTrace(BoundExpr expr, object? value)
    {
        if (_traceSteps == null)
            return;

        _traceSteps.Add(new EvaluationTraceStep(
            expr.GetType().Name,
            value,
            value?.ToString()));
    }

    private object? MatchPattern(object? value, Pattern pattern)
        => PatternRuntime.MatchPattern(value, pattern, _context, _options, _cancellationToken);
}
