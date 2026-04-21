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
    /// Asynchronously evaluates an expression string.
    /// Use this API for expressions that may contain <c>await</c>.
    /// </summary>
    /// <param name="expression">Expression source to evaluate.</param>
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
    /// Asynchronously evaluates a pre-parsed expression.
    /// Use this API for expressions that may contain <c>await</c>.
    /// </summary>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>A task that represents the evaluation result.</returns>
    public async ValueTask<object?> EvaluateAsync(
        AlderExpression expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        using var state = CreateEvaluationState(variables, cancellationToken);
        return await EvaluateAsyncCore(expression, state, cancellationToken);
    }

    /// <summary>
    /// Asynchronously evaluates an expression with variables supplied as an anonymous object.
    /// Property types are preserved for binding.
    /// </summary>
    public ValueTask<object?> EvaluateAsync(
        string expression,
        object variables,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var parsed = Parse(expression);
        return EvaluateAsync(parsed, variables, cancellationToken);
    }

    /// <summary>
    /// Asynchronously evaluates a pre-parsed expression with variables supplied as an anonymous object.
    /// </summary>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>A task that represents the evaluation result.</returns>
    public async ValueTask<object?> EvaluateAsync(
        AlderExpression expression,
        object variables,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        using var state = CreateEvaluationState(VariableBindingProjector.ProjectTypedVariables(variables), cancellationToken);
        return await EvaluateAsyncCore(expression, state, cancellationToken);
    }

    /// <summary>
    /// Asynchronously evaluates an expression and converts the result to <typeparamref name="T"/>.
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
    /// Asynchronously evaluates a pre-parsed expression and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public async ValueTask<T?> EvaluateAsync<T>(
        AlderExpression expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        var result = await EvaluateAsync(expression, variables, cancellationToken);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// Asynchronously evaluates an expression with anonymous object variables and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public async ValueTask<T?> EvaluateAsync<T>(
        string expression,
        object variables,
        CancellationToken cancellationToken = default)
    {
        var result = await EvaluateAsync(expression, variables, cancellationToken);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// Asynchronously evaluates a pre-parsed expression with anonymous object variables and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public async ValueTask<T?> EvaluateAsync<T>(
        AlderExpression expression,
        object variables,
        CancellationToken cancellationToken = default)
    {
        var result = await EvaluateAsync(expression, variables, cancellationToken);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// Asynchronously evaluates a C# expression with inline variables.
    /// Variables are accessible as <c>@0</c>, <c>@1</c>, etc. by position.
    /// Dictionaries and objects are also destructured into named variables.
    /// </summary>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="variables">Variables accessible within the expression.</param>
    /// <returns>A task that represents the evaluation result.</returns>
    public ValueTask<object?> EvaluateAsync(string expression, params object?[] variables)
    {
        if (variables.Length == 0)
            return EvaluateAsync(expression, (IDictionary<string, object?>?)null);

        return EvaluateAsync(expression, VariableBindingProjector.BuildPositionalVariables(variables));
    }

    /// <summary>
    /// Asynchronously evaluates a C# expression with inline variables and converts the result to <typeparamref name="T"/>.
    /// Variables are accessible as <c>@0</c>, <c>@1</c>, etc. by position.
    /// Dictionaries and objects are also destructured into named variables.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="variables">Variables accessible within the expression.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public async ValueTask<T?> EvaluateAsync<T>(string expression, params object?[] variables)
    {
        var result = await EvaluateAsync(expression, variables);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// Asynchronously evaluates a pre-parsed expression with inline variables.
    /// Variables are accessible as <c>@0</c>, <c>@1</c>, etc. by position.
    /// Dictionaries and objects are also destructured into named variables.
    /// </summary>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">Variables accessible within the expression.</param>
    /// <returns>A task that represents the evaluation result.</returns>
    public ValueTask<object?> EvaluateAsync(AlderExpression expression, params object?[] variables)
    {
        if (variables.Length == 0)
            return EvaluateAsync(expression, (IDictionary<string, object?>?)null);

        return EvaluateAsync(expression, VariableBindingProjector.BuildPositionalVariables(variables));
    }

    /// <summary>
    /// Asynchronously evaluates a pre-parsed expression with inline variables and converts the result to <typeparamref name="T"/>.
    /// Variables are accessible as <c>@0</c>, <c>@1</c>, etc. by position.
    /// Dictionaries and objects are also destructured into named variables.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">Variables accessible within the expression.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public async ValueTask<T?> EvaluateAsync<T>(AlderExpression expression, params object?[] variables)
    {
        var result = await EvaluateAsync(expression, variables);
        return ConvertResult<T>(result);
    }

    private async ValueTask<object?> EvaluateAsyncCore(
        AlderExpression expression,
        EvaluationStateLease state,
        CancellationToken cancellationToken)
    {
        if (expression.TryGetOrCreateBoundExpression(state.BindingContext, out var boundExpression, out var boundFailureReason))
        {
            if (boundExpression != null)
            {
                try
                {
                    var processed = _pipelineCache.GetValue(boundExpression,
                        b => RunPipeline(b, cancellationToken));

                    var boundEvaluator = new BoundEvaluator(
                        state.ExecutionContext,
                        state.ConstraintState,
                        sourceText: new SourceText(expression.Source));

                    var boundResult = await boundEvaluator.EvaluateAsync(processed, cancellationToken);
                    expression.RecordBoundExecution();
                    return UnwrapControlFlowSignal(boundResult);
                }
                catch (BindingNotSupportedException ex)
                {
                    expression.RecordBoundFallback(ex.Message);
                    throw new AlderException(DiagnosticDescriptors.BindingFailed, ex, ex.Message);
                }
            }
        }

        expression.RecordBoundFallback(boundFailureReason);
        throw new AlderException(DiagnosticDescriptors.BindingFailed, boundFailureReason ?? "Binding failed for expression.");
    }
}
