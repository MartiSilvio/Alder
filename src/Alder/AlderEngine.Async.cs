using System.Threading.Tasks;
using Alder.Binding;
using Alder.Diagnostics;
using Alder.Interpretation;
using Alder.Runtime;
using Alder.Text;

namespace Alder;

public sealed partial class AlderEngine
{
    /// <summary>
    /// Asynchronously evaluates a C# expression string and returns the result.
    /// Required for expressions containing <c>await</c>. Also works for non-async expressions.
    /// </summary>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>A task that represents the evaluation result.</returns>
    public ValueTask<object?> EvaluateAsync(
        string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var parsed = Parse(expression);
        return EvaluateAsync(parsed, variables, cancellationToken);
    }

    /// <summary>
    /// Asynchronously evaluates a pre-parsed expression and returns the result.
    /// Required for expressions containing <c>await</c>. Also works for non-async expressions.
    /// </summary>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>A task that represents the evaluation result.</returns>
    public ValueTask<object?> EvaluateAsync(
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
        var constraintState = new ExecutionConstraintState();
        constraintState.Reset(_config.Constraints);

        var executionContext = context.CreateChild();
        return EvaluateAsyncCore(expression, context, executionContext, constraintState, cancellationToken);
    }

    /// <summary>
    /// Asynchronously evaluates a C# expression with variables supplied as an anonymous object.
    /// Property types are preserved for type-aware binding.
    /// </summary>
    public ValueTask<object?> EvaluateAsync(
        string expression,
        object variables,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var child = CreateChild();
        child.SetTypedVariablesFromObject(variables);
        return child.EvaluateAsync(expression, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Asynchronously evaluates a C# expression and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public async ValueTask<T?> EvaluateAsync<T>(
        string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        var result = await EvaluateAsync(expression, variables, cancellationToken);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// Asynchronously evaluates a C# expression with anonymous object variables and converts to <typeparamref name="T"/>.
    /// </summary>
    public async ValueTask<T?> EvaluateAsync<T>(
        string expression,
        object variables,
        CancellationToken cancellationToken = default)
    {
        var result = await EvaluateAsync(expression, variables, cancellationToken);
        return ConvertResult<T>(result);
    }

    private async ValueTask<object?> EvaluateAsyncCore(
        AlderExpression expression,
        AlderContext bindingContext,
        AlderContext executionContext,
        ExecutionConstraintState constraintState,
        CancellationToken cancellationToken)
    {
        if (expression.TryGetOrCreateBoundExpression(bindingContext, out var boundExpression, out var boundFailureReason))
        {
            if (boundExpression != null)
            {
                try
                {
                    var processed = _pipelineCache.GetValue(boundExpression,
                        b => RunPipeline(b, cancellationToken));

                    var boundEvaluator = new BoundEvaluator(
                        executionContext, constraintState,
                        sourceText: new SourceText(expression.Source));

                    var boundResult = await boundEvaluator.EvaluateAsync(processed, cancellationToken);
                    expression.RecordBoundExecution();
                    return UnwrapControlFlowSignal(boundResult);
                }
                catch (BindingNotSupportedException ex)
                {
                    expression.RecordBoundFallback(ex.Message);
                    throw new AlderException(DiagnosticDescriptors.BindingFailed, ex.Message);
                }
            }
        }

        expression.RecordBoundFallback(boundFailureReason);
        throw new AlderException(DiagnosticDescriptors.BindingFailed, boundFailureReason ?? "Binding failed for expression.");
    }
}
