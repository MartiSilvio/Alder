using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Interpretation.Evaluators;
using Alder.Runtime;
using Alder.Runtime.Semantics;
using Alder.Text;
using Alder.Tracing;

namespace Alder.Interpretation;

internal sealed class BoundEvaluator
{
    private AlderContext _context
    {
        get => _evalCtx.Context;
        set => _evalCtx.Context = value;
    }
    private readonly AlderConfig _config;
    private readonly ExecutionConstraintState? _constraintState;
    private readonly CancellationToken _cancellationToken;

    private readonly EvaluationTracer? _tracer;
    private readonly SourceText? _sourceText;
    private readonly Stack<Exception> _caughtExceptions = new();

    private readonly EvaluationContext _evalCtx;

    public BoundEvaluator(
        AlderContext context,
        AlderConfig config,
        ExecutionConstraintState? constraintState = null,
        EvaluationTracer? tracer = null,
        SourceText? sourceText = null,
        CancellationToken cancellationToken = default)
    {
        _config = config;
        _constraintState = constraintState;
        _cancellationToken = cancellationToken;
        _tracer = tracer;
        _sourceText = sourceText;
        _evalCtx = new EvaluationContext(context, config, constraintState, cancellationToken, _caughtExceptions, Evaluate);
        _evalCtx.Tracer = tracer;
        _evalCtx.Register(BoundNodeKind.Literal, new LiteralEvaluator());
        _evalCtx.Register(BoundNodeKind.Identifier, new IdentifierEvaluator());
        _evalCtx.Register(BoundNodeKind.Conversion, new CastEvaluator());
        _evalCtx.Register(BoundNodeKind.AsOperator, new AsEvaluator());
        _evalCtx.Register(BoundNodeKind.IsPatternExpression, new IsPatternEvaluator());
        _evalCtx.Register(BoundNodeKind.CollectionCreation, new CollectionCreationEvaluator());
        _evalCtx.Register(BoundNodeKind.ObjectLiteral, new ObjectLiteralEvaluator());
        _evalCtx.Register(BoundNodeKind.SpreadElement, new SpreadEvaluator());
        _evalCtx.Register(BoundNodeKind.SliceExpression, new SliceEvaluator());
        _evalCtx.Register(BoundNodeKind.ObjectCreationExpression, new ObjectCreationEvaluator());
        _evalCtx.Register(BoundNodeKind.ArrayAllocation, new ArrayAllocationEvaluator());
        _evalCtx.Register(BoundNodeKind.MultiDimArrayInit, new MultiDimArrayInitEvaluator());
        _evalCtx.Register(BoundNodeKind.TupleLiteral, new TupleEvaluator());
        _evalCtx.Register(BoundNodeKind.DeconstructionAssignment, new DeconstructionEvaluator());
        _evalCtx.Register(BoundNodeKind.InterpolatedString, new InterpolatedStringEvaluator());
        _evalCtx.Register(BoundNodeKind.UnaryOperator, new UnaryEvaluator());
        _evalCtx.Register(BoundNodeKind.BinaryOperator, new BinaryEvaluator());
        _evalCtx.Register(BoundNodeKind.LogicalOperator, new LogicalEvaluator());
        _evalCtx.Register(BoundNodeKind.NullCoalescingOperator, new NullCoalesceEvaluator());
        _evalCtx.Register(BoundNodeKind.ConditionalOperator, new ConditionalEvaluator());
        _evalCtx.Register(BoundNodeKind.Block, new BlockEvaluator());
        _evalCtx.Register(BoundNodeKind.IfStatement, new IfEvaluator());
        _evalCtx.Register(BoundNodeKind.WhileStatement, new WhileEvaluator());
        _evalCtx.Register(BoundNodeKind.ForStatement, new ForEvaluator());
        _evalCtx.Register(BoundNodeKind.DoStatement, new DoWhileEvaluator());
        _evalCtx.Register(BoundNodeKind.ForEachStatement, new ForEachEvaluator());
        _evalCtx.Register(BoundNodeKind.UsingStatement, new UsingEvaluator());
        _evalCtx.Register(BoundNodeKind.LockStatement, new LockEvaluator());
        _evalCtx.Register(BoundNodeKind.SwitchStatement, new SwitchStatementEvaluator());
        _evalCtx.Register(BoundNodeKind.SwitchExpression, new SwitchExpressionEvaluator());
        _evalCtx.Register(BoundNodeKind.CheckedExpression, new CheckedEvaluator());
        _evalCtx.Register(BoundNodeKind.ChainedComparisonOperator, new ChainedComparisonEvaluator());
        _evalCtx.Register(BoundNodeKind.BreakStatement, new BreakEvaluator());
        _evalCtx.Register(BoundNodeKind.ContinueStatement, new ContinueEvaluator());
        _evalCtx.Register(BoundNodeKind.GotoStatement, new GotoEvaluator());
        _evalCtx.Register(BoundNodeKind.GotoCaseStatement, new GotoCaseEvaluator());
        _evalCtx.Register(BoundNodeKind.GotoDefaultStatement, new GotoDefaultEvaluator());
        _evalCtx.Register(BoundNodeKind.Label, new LabelEvaluator());
        _evalCtx.Register(BoundNodeKind.VariableDeclaration, new VariableDeclEvaluator());
        _evalCtx.Register(BoundNodeKind.AssignmentOperator, new AssignEvaluator());
        _evalCtx.Register(BoundNodeKind.NullCoalescingAssignmentOperator, new NullCoalesceAssignEvaluator());
        _evalCtx.Register(BoundNodeKind.CompoundAssignmentOperator, new CompoundAssignEvaluator());
        _evalCtx.Register(BoundNodeKind.IncrementOperator, new IncrementDecrementEvaluator());
        _evalCtx.Register(BoundNodeKind.MemberAssignment, new MemberAssignEvaluator());
        _evalCtx.Register(BoundNodeKind.IndexAssignment, new IndexAssignEvaluator());
        _evalCtx.Register(BoundNodeKind.MemberCompoundAssignment, new MemberCompoundAssignEvaluator());
        _evalCtx.Register(BoundNodeKind.IndexCompoundAssignment, new IndexCompoundAssignEvaluator());
        _evalCtx.Register(BoundNodeKind.MemberNullCoalesceAssignment, new MemberNullCoalesceAssignEvaluator());
        _evalCtx.Register(BoundNodeKind.IndexNullCoalesceAssignment, new IndexNullCoalesceAssignEvaluator());
        _evalCtx.Register(BoundNodeKind.MemberIncrement, new MemberIncrementEvaluator());
        _evalCtx.Register(BoundNodeKind.IndexIncrement, new IndexIncrementEvaluator());
        _evalCtx.Register(BoundNodeKind.ThrowExpression, new ThrowEvaluator());
        _evalCtx.Register(BoundNodeKind.TryStatement, new TryCatchEvaluator());
        _evalCtx.Register(BoundNodeKind.ReturnStatement, new ReturnEvaluator());
        _evalCtx.Register(BoundNodeKind.PropertyAccess, new PropertyAccessEvaluator());
        _evalCtx.Register(BoundNodeKind.FieldAccess, new FieldAccessEvaluator());
        _evalCtx.Register(BoundNodeKind.MethodGroup, new MethodGroupEvaluator());
        _evalCtx.Register(BoundNodeKind.DynamicMemberAccess, new DynamicMemberAccessEvaluator());
        _evalCtx.Register(BoundNodeKind.ResolvedIndexAccess, new ResolvedIndexAccessEvaluator());
        _evalCtx.Register(BoundNodeKind.DynamicIndexAccess, new DynamicIndexAccessEvaluator());
        _evalCtx.Register(BoundNodeKind.ResolvedMultiDimIndexAccess, new ResolvedMultiDimIndexAccessEvaluator());
        _evalCtx.Register(BoundNodeKind.DynamicMultiDimIndexAccess, new DynamicMultiDimIndexAccessEvaluator());
        _evalCtx.Register(BoundNodeKind.MultiDimIndexAssignment, new MultiDimIndexAssignEvaluator());
        _evalCtx.Register(BoundNodeKind.NamedArgument, new NamedArgumentEvaluator());
        _evalCtx.Register(BoundNodeKind.OutArgument, new OutArgEvaluator());
        _evalCtx.Register(BoundNodeKind.ResolvedCall, new ResolvedCallEvaluator());
        _evalCtx.Register(BoundNodeKind.DynamicCall, new DynamicCallEvaluator());
        _evalCtx.Register(BoundNodeKind.Lambda, new LambdaEvaluator());
        _evalCtx.Register(BoundNodeKind.PipelineExpression, new PipelineEvaluator());
        _evalCtx.Register(BoundNodeKind.RangeExpression, new RangeEvaluator());
        _evalCtx.Register(BoundNodeKind.FromEndIndexExpression, new FromEndIndexEvaluator());
    }

    public object? Evaluate(BoundExpr expr)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        _tracer?.Push(expr);
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
            _tracer?.PopError(ex);
            throw;
        }
        catch (Exception ex)
        {
            _tracer?.PopError(ex);
            throw;
        }

        _tracer?.Pop(result);
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

        if (_evalCtx.TryEvaluate(expr, out var result))
            return result;

        throw new BindingNotSupportedException(
            $"Bound execution for node '{expr.GetType().Name}' is not implemented");
    }
}
