using Alder.Diagnostics;
using Alder.Interpretation;
using Alder.Runtime;
using Alder.Tracing;

namespace Alder;

public sealed partial class AlderEngine
{
    /// <summary>
    /// Evaluates an expression and returns both the result and a step-by-step evaluation trace tree.
    /// </summary>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>An <see cref="EvaluationTraceResult"/> containing the result, the trace tree, and any exception.</returns>
    /// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
    /// <exception cref="AlderException">The expression contains syntax or binding errors.</exception>
    public EvaluationTraceResult EvaluateWithTrace(
        string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var parsed = Parse(expression);
        return EvaluateWithTrace(parsed, variables, cancellationToken);
    }

    /// <summary>
    /// Evaluates a pre-parsed expression and returns both the result and a step-by-step evaluation trace tree.
    /// </summary>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>An <see cref="EvaluationTraceResult"/> containing the result, the trace tree, and any exception.</returns>
    /// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
    /// <exception cref="AlderException">The expression contains binding errors.</exception>
    public EvaluationTraceResult EvaluateWithTrace(
        AlderExpression expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        using var state = CreateEvaluationState(variables, cancellationToken);

        if (!TryGetOrCreateBoundExpression(expression, state.BindingContext, out var boundExpression, out var failureReason) ||
            boundExpression == null)
        {
            RecordBoundFallback(expression, failureReason);
            throw new AlderException(DiagnosticDescriptors.BindingFailed, failureReason ?? "Binding failed for expression.");
        }

        boundExpression = RunSecurityOnlyPipeline(boundExpression, cancellationToken);
        var sourceText = new Text.SourceText(expression.Source);
        var tracer = new EvaluationTracer(sourceText);
        var evaluator = new BoundEvaluator(state.ExecutionContext, state.ConstraintState, tracer, sourceText);

        try
        {
            var result = evaluator.Evaluate(boundExpression, cancellationToken);
            RecordBoundExecution(expression);
            return new EvaluationTraceResult(UnwrapControlFlowSignal(result), tracer.Root!, null);
        }
        catch (Exception ex)
        {
            return new EvaluationTraceResult(null, tracer.Root!, ex);
        }
    }
}
