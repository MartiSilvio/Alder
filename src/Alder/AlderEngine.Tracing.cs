using Alder.Diagnostics;
using Alder.Interpretation;
using Alder.Runtime;
using Alder.Tracing;

namespace Alder;

public sealed partial class AlderEngine
{
    public EvaluationTraceResult EvaluateWithTrace(
        string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var parsed = Parse(expression);
        return EvaluateWithTrace(parsed, variables, cancellationToken);
    }

    public EvaluationTraceResult EvaluateWithTrace(
        AlderExpression expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var target = this;
        if (variables != null)
        {
            target = CreateChild();
            target.SetVariables(variables);
        }

        var context = target.GetOrCreateContext();
        var executionContext = context;

        var constraints = _options.Constraints;
        if (constraints != null)
        {
            executionContext = context.CreateChild();
            var state = new ExecutionConstraintState();
            state.Reset(constraints);
            executionContext.ConstraintState = state;
        }

        if (!expression.TryGetOrCreateBoundExpression(executionContext, _options.MaxExpressionDepth, out var boundExpression, out var failureReason) ||
            boundExpression == null)
        {
            expression.RecordBoundFallback(failureReason);
            if (IsDepthFailure(failureReason))
                throw new AlderDepthException("binding", _options.MaxExpressionDepth);
            throw new AlderException(DiagnosticDescriptors.BindingFailed, failureReason ?? "Binding failed for expression.");
        }

        boundExpression = RunPipeline(boundExpression, cancellationToken);
        var sourceText = new Text.SourceText(expression.Source);
        var tracer = new EvaluationTracer(sourceText);
        var evaluator = new BoundEvaluator(executionContext, _options, cancellationToken, tracer, sourceText);

        try
        {
            var result = evaluator.Evaluate(boundExpression);
            expression.RecordBoundExecution();
            return new EvaluationTraceResult(UnwrapControlFlowSignal(result), tracer.Root!, null);
        }
        catch (Exception ex)
        {
            return new EvaluationTraceResult(null, tracer.Root!, ex);
        }
    }
}
