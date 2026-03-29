using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Interpretation.Evaluators;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Runtime.Semantics;
using Alder.Text;
using Alder.Tracing;

namespace Alder.Interpretation;

internal sealed class EvaluationContext
{
    public AlderContext Context
    {
        get => _contextRef;
        set => _contextRef = value;
    }

    public AlderConfig Config { get; }
    public ExecutionConstraintState? ConstraintState { get; }
    public CancellationToken CancellationToken { get; }
    public Stack<Exception> CaughtExceptions { get; }

    public EvaluationTracer? Tracer { get; set; }
    public SourceText? SourceText { get; set; }
    public int BreakContextDepth { get; set; }
    public int LoopDepth { get; set; }
    public bool IsChecked { get; set; }

    private AlderContext _contextRef;

    internal EvaluationContext(
        AlderContext context,
        AlderConfig config,
        ExecutionConstraintState? constraintState,
        CancellationToken cancellationToken,
        Stack<Exception> caughtExceptions)
    {
        _contextRef = context;
        Config = config;
        ConstraintState = constraintState;
        CancellationToken = cancellationToken;
        CaughtExceptions = caughtExceptions;
    }

    public object? Evaluate(BoundExpr expr)
    {
        CancellationToken.ThrowIfCancellationRequested();

        Tracer?.Push(expr);
        object? result;
        try
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

            result = expr.Kind switch
            {
                BoundNodeKind.Literal => LiteralEvaluator.Evaluate((BoundLiteralExpr)expr, this),
                BoundNodeKind.Identifier => IdentifierEvaluator.Evaluate((BoundIdentifierExpr)expr, this),
                BoundNodeKind.Conversion => CastEvaluator.Evaluate((BoundCastExpr)expr, this),
                BoundNodeKind.AsOperator => AsEvaluator.Evaluate((BoundAsExpr)expr, this),
                BoundNodeKind.IsPatternExpression => IsPatternEvaluator.Evaluate((BoundIsPatternExpr)expr, this),
                BoundNodeKind.CollectionCreation => CollectionCreationEvaluator.Evaluate((BoundCollectionCreationExpr)expr, this),
                BoundNodeKind.ObjectLiteral => ObjectLiteralEvaluator.Evaluate((BoundObjectLiteralExpr)expr, this),
                BoundNodeKind.SpreadElement => SpreadEvaluator.Evaluate((BoundSpreadExpr)expr, this),
                BoundNodeKind.SliceExpression => SliceEvaluator.Evaluate((BoundSliceExpr)expr, this),
                BoundNodeKind.ObjectCreationExpression => ObjectCreationEvaluator.Evaluate((BoundObjectCreationExpr)expr, this),
                BoundNodeKind.ArrayAllocation => ArrayAllocationEvaluator.Evaluate((BoundArrayAllocationExpr)expr, this),
                BoundNodeKind.MultiDimArrayInit => MultiDimArrayInitEvaluator.Evaluate((BoundMultiDimArrayInitExpr)expr, this),
                BoundNodeKind.TupleLiteral => TupleEvaluator.Evaluate((BoundTupleExpr)expr, this),
                BoundNodeKind.DeconstructionAssignment => DeconstructionEvaluator.Evaluate((BoundDeconstructionExpr)expr, this),
                BoundNodeKind.InterpolatedString => InterpolatedStringEvaluator.Evaluate((BoundInterpolatedStringExpr)expr, this),
                BoundNodeKind.UnaryOperator => UnaryEvaluator.Evaluate((BoundUnaryExpr)expr, this),
                BoundNodeKind.BinaryOperator => BinaryEvaluator.Evaluate((BoundBinaryExpr)expr, this),
                BoundNodeKind.LogicalOperator => LogicalEvaluator.Evaluate((BoundLogicalExpr)expr, this),
                BoundNodeKind.NullCoalescingOperator => NullCoalesceEvaluator.Evaluate((BoundNullCoalesceExpr)expr, this),
                BoundNodeKind.ConditionalOperator => ConditionalEvaluator.Evaluate((BoundConditionalExpr)expr, this),
                BoundNodeKind.Block => BlockEvaluator.Evaluate((BoundBlockExpr)expr, this),
                BoundNodeKind.IfStatement => IfEvaluator.Evaluate((BoundIfStatementExpr)expr, this),
                BoundNodeKind.WhileStatement => WhileEvaluator.Evaluate((BoundWhileExpr)expr, this),
                BoundNodeKind.ForStatement => ForEvaluator.Evaluate((BoundForExpr)expr, this),
                BoundNodeKind.DoStatement => DoWhileEvaluator.Evaluate((BoundDoWhileExpr)expr, this),
                BoundNodeKind.ForEachStatement => ForEachEvaluator.Evaluate((BoundForEachExpr)expr, this),
                BoundNodeKind.UsingStatement => UsingEvaluator.Evaluate((BoundUsingStatementExpr)expr, this),
                BoundNodeKind.LockStatement => LockEvaluator.Evaluate((BoundLockStatementExpr)expr, this),
                BoundNodeKind.SwitchStatement => SwitchStatementEvaluator.Evaluate((BoundSwitchStatementExpr)expr, this),
                BoundNodeKind.SwitchExpression => SwitchExpressionEvaluator.Evaluate((BoundSwitchExpressionExpr)expr, this),
                BoundNodeKind.CheckedExpression => CheckedEvaluator.Evaluate((BoundCheckedExpr)expr, this),
                BoundNodeKind.ChainedComparisonOperator => ChainedComparisonEvaluator.Evaluate((BoundChainedComparisonExpr)expr, this),
                BoundNodeKind.BreakStatement => BreakEvaluator.Evaluate((BoundBreakExpr)expr, this),
                BoundNodeKind.ContinueStatement => ContinueEvaluator.Evaluate((BoundContinueExpr)expr, this),
                BoundNodeKind.GotoStatement => GotoEvaluator.Evaluate((BoundGotoExpr)expr, this),
                BoundNodeKind.GotoCaseStatement => GotoCaseEvaluator.Evaluate((BoundGotoCaseExpr)expr, this),
                BoundNodeKind.GotoDefaultStatement => GotoDefaultEvaluator.Evaluate((BoundGotoDefaultExpr)expr, this),
                BoundNodeKind.Label => LabelEvaluator.Evaluate((BoundLabelExpr)expr, this),
                BoundNodeKind.VariableDeclaration => VariableDeclEvaluator.Evaluate((BoundVariableDeclExpr)expr, this),
                BoundNodeKind.AssignmentOperator => AssignEvaluator.Evaluate((BoundAssignExpr)expr, this),
                BoundNodeKind.NullCoalescingAssignmentOperator => NullCoalesceAssignEvaluator.Evaluate((BoundNullCoalesceAssignExpr)expr, this),
                BoundNodeKind.CompoundAssignmentOperator => CompoundAssignEvaluator.Evaluate((BoundCompoundAssignExpr)expr, this),
                BoundNodeKind.IncrementOperator => IncrementDecrementEvaluator.Evaluate((BoundIncrementDecrementExpr)expr, this),
                BoundNodeKind.MemberAssignment => MemberAssignEvaluator.Evaluate((BoundMemberAssignExpr)expr, this),
                BoundNodeKind.IndexAssignment => IndexAssignEvaluator.Evaluate((BoundIndexAssignExpr)expr, this),
                BoundNodeKind.MemberCompoundAssignment => MemberCompoundAssignEvaluator.Evaluate((BoundMemberCompoundAssignExpr)expr, this),
                BoundNodeKind.IndexCompoundAssignment => IndexCompoundAssignEvaluator.Evaluate((BoundIndexCompoundAssignExpr)expr, this),
                BoundNodeKind.MemberNullCoalesceAssignment => MemberNullCoalesceAssignEvaluator.Evaluate((BoundMemberNullCoalesceAssignExpr)expr, this),
                BoundNodeKind.IndexNullCoalesceAssignment => IndexNullCoalesceAssignEvaluator.Evaluate((BoundIndexNullCoalesceAssignExpr)expr, this),
                BoundNodeKind.MemberIncrement => MemberIncrementEvaluator.Evaluate((BoundMemberIncrementExpr)expr, this),
                BoundNodeKind.IndexIncrement => IndexIncrementEvaluator.Evaluate((BoundIndexIncrementExpr)expr, this),
                BoundNodeKind.ThrowExpression => ThrowEvaluator.Evaluate((BoundThrowExpr)expr, this),
                BoundNodeKind.TryStatement => TryCatchEvaluator.Evaluate((BoundTryCatchFinallyExpr)expr, this),
                BoundNodeKind.ReturnStatement => ReturnEvaluator.Evaluate((BoundReturnExpr)expr, this),
                BoundNodeKind.PropertyAccess => PropertyAccessEvaluator.Evaluate((BoundPropertyAccessExpr)expr, this),
                BoundNodeKind.FieldAccess => FieldAccessEvaluator.Evaluate((BoundFieldAccessExpr)expr, this),
                BoundNodeKind.MethodGroup => MethodGroupEvaluator.Evaluate((BoundMethodGroupExpr)expr, this),
                BoundNodeKind.DynamicMemberAccess => DynamicMemberAccessEvaluator.Evaluate((BoundDynamicMemberAccessExpr)expr, this),
                BoundNodeKind.ResolvedIndexAccess => ResolvedIndexAccessEvaluator.Evaluate((BoundResolvedIndexAccessExpr)expr, this),
                BoundNodeKind.DynamicIndexAccess => DynamicIndexAccessEvaluator.Evaluate((BoundDynamicIndexAccessExpr)expr, this),
                BoundNodeKind.ResolvedMultiDimIndexAccess => ResolvedMultiDimIndexAccessEvaluator.Evaluate((BoundResolvedMultiDimIndexAccessExpr)expr, this),
                BoundNodeKind.DynamicMultiDimIndexAccess => DynamicMultiDimIndexAccessEvaluator.Evaluate((BoundDynamicMultiDimIndexAccessExpr)expr, this),
                BoundNodeKind.MultiDimIndexAssignment => MultiDimIndexAssignEvaluator.Evaluate((BoundMultiDimIndexAssignExpr)expr, this),
                BoundNodeKind.NamedArgument => NamedArgumentEvaluator.Evaluate((BoundNamedArgumentExpr)expr, this),
                BoundNodeKind.OutArgument => OutArgEvaluator.Evaluate((BoundOutArgExpr)expr, this),
                BoundNodeKind.ResolvedCall => ResolvedCallEvaluator.Evaluate((BoundResolvedCallExpr)expr, this),
                BoundNodeKind.DynamicCall => DynamicCallEvaluator.Evaluate((BoundDynamicCallExpr)expr, this),
                BoundNodeKind.Lambda => LambdaEvaluator.Evaluate((BoundLambdaExpr)expr, this),
                BoundNodeKind.PipelineExpression => PipelineEvaluator.Evaluate((BoundPipelineExpr)expr, this),
                BoundNodeKind.RangeExpression => RangeEvaluator.Evaluate((BoundRangeExpr)expr, this),
                BoundNodeKind.FromEndIndexExpression => FromEndIndexEvaluator.Evaluate((BoundIndexFromEndExpr)expr, this),
                _ => throw new BindingNotSupportedException(
                    $"Bound execution for node '{expr.GetType().Name}' is not implemented")
            };
        }
        catch (AlderException ex) when (ex.Span.IsEmpty && !expr.Span.IsEmpty)
        {
            int? line = null, column = null;
            if (SourceText != null)
            {
                var pos = SourceText.GetLinePosition(expr.Span.Start);
                line = pos.Line + 1;
                column = pos.Character + 1;
            }
            ex.EnrichDiagnosticsWithPosition(expr.Span, line, column);
            Tracer?.PopError(ex);
            throw;
        }
        catch (Exception ex)
        {
            Tracer?.PopError(ex);
            throw;
        }

        Tracer?.Pop(result);
        return result;
    }

    public object? MatchPattern(object? value, Pattern pattern)
        => PatternRuntime.MatchPattern(value, pattern, Context, Config, CancellationToken);
}
