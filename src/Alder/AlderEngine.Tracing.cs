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
    /// <param name="expression">The C# expression string to evaluate.</param>
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

        var target = this;
        if (variables != null)
        {
            target = CreateChild();
            target.SetVariables(variables);
        }

        var context = target.GetOrCreateContext();
        var executionContext = context;

        var constraints = _config.Constraints;
        executionContext = context.CreateChild();
        var state = new ExecutionConstraintState();
        state.Reset(constraints);

        if (!expression.TryGetOrCreateBoundExpression(executionContext, _config.Constraints.MaxExpressionDepth, out var boundExpression, out var failureReason) ||
            boundExpression == null)
        {
            expression.RecordBoundFallback(failureReason);
            throw new AlderException(DiagnosticDescriptors.BindingFailed, failureReason ?? "Binding failed for expression.");
        }

        boundExpression = RunPipeline(boundExpression, cancellationToken);
        var sourceText = new Text.SourceText(expression.Source);
        var tracer = new EvaluationTracer(sourceText);
        var evaluator = new BoundEvaluator(executionContext, _config, state, tracer, sourceText, cancellationToken);

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
