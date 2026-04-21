using Alder.Binding;
using Alder.Diagnostics;
using Alder.Interpretation;
using Alder.Runtime;

namespace Alder;

public sealed partial class AlderEngine
{
    /// <summary>
    /// Evaluates source text and returns the result.
    /// </summary>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    /// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
    /// <exception cref="AlderException">The expression contains errors or evaluation fails.</exception>
    public object? Evaluate(
        string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var parsed = Parse(expression);
        return Evaluate(parsed, variables, cancellationToken);
    }

    /// <summary>
    /// Evaluates a previously parsed expression and returns the result.
    /// </summary>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    /// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
    /// <exception cref="AlderException">Evaluation fails due to binding or runtime errors.</exception>
    public object? Evaluate(
        AlderExpression expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        if (expression is null) throw new ArgumentNullException(nameof(expression));
        ThrowIfDisposed();
        using var state = CreateEvaluationState(variables, cancellationToken);

        try
        {
            if (_config.Compiler != null)
            {
                return ExecuteCompiledExpression(
                    expression,
                    state.BindingContext,
                    state.ExecutionContext,
                    state.ConstraintState,
                    cancellationToken);
            }

            return EvaluateCore(expression, state, cancellationToken);
        }
        catch (InsufficientExecutionStackException ex)
        {
            throw new AlderException(DiagnosticDescriptors.ExpressionNestingDepthExceeded, ex);
        }
    }

    private object? EvaluateCore(
        AlderExpression expression,
        EvaluationStateLease state,
        CancellationToken cancellationToken)
    {
        if (TryGetOrCreateBoundExpression(expression, state.BindingContext, out var boundExpression, out var boundFailureReason))
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
                        sourceText: new Text.SourceText(expression.Source));

                    var boundResult = boundEvaluator.Evaluate(processed, cancellationToken);
                    RecordBoundExecution(expression);
                    return UnwrapControlFlowSignal(boundResult);
                }
                catch (BindingNotSupportedException ex)
                {
                    RecordBoundFallback(expression, ex.Message);
                    throw new AlderException(DiagnosticDescriptors.BindingFailed, ex, ex.Message);
                }
            }
        }

        RecordBoundFallback(expression, boundFailureReason);
        throw new AlderException(DiagnosticDescriptors.BindingFailed, boundFailureReason ?? "Binding failed for expression.");
    }

    /// <summary>
    /// Evaluates a C# expression with variables supplied as an anonymous object.
    /// </summary>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    public object? Evaluate(
        string expression,
        object variables,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var parsed = Parse(expression);
        return Evaluate(parsed, variables, cancellationToken);
    }

    /// <summary>
    /// Evaluates a pre-parsed expression with variables supplied as an anonymous object.
    /// </summary>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    public object? Evaluate(
        AlderExpression expression,
        object variables,
        CancellationToken cancellationToken = default)
    {
        if (expression is null) throw new ArgumentNullException(nameof(expression));
        ThrowIfDisposed();
        using var state = CreateEvaluationState(VariableBindingProjector.ProjectTypedVariables(variables), cancellationToken);

        try
        {
            if (_config.Compiler != null)
            {
                return ExecuteCompiledExpression(
                    expression,
                    state.BindingContext,
                    state.ExecutionContext,
                    state.ConstraintState,
                    cancellationToken);
            }

            return EvaluateCore(expression, state, cancellationToken);
        }
        catch (InsufficientExecutionStackException ex)
        {
            throw new AlderException(DiagnosticDescriptors.ExpressionNestingDepthExceeded, ex);
        }
    }

    /// <summary>
    /// Evaluates a C# expression and converts the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public T? Evaluate<T>(
        string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        var result = Evaluate(expression, variables, cancellationToken);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// Evaluates a pre-parsed expression and converts the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public T? Evaluate<T>(
        AlderExpression expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        var result = Evaluate(expression, variables, cancellationToken);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// Evaluates a C# expression with anonymous object variables and converts the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public T? Evaluate<T>(
        string expression,
        object variables,
        CancellationToken cancellationToken = default)
    {
        var result = Evaluate(expression, variables, cancellationToken);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// Evaluates a pre-parsed expression with anonymous object variables and converts the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public T? Evaluate<T>(
        AlderExpression expression,
        object variables,
        CancellationToken cancellationToken = default)
    {
        var result = Evaluate(expression, variables, cancellationToken);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// Evaluates a C# expression with inline variables.
    /// Variables are accessible as <c>@0</c>, <c>@1</c>, etc. by position.
    /// Dictionaries and objects are also destructured into named variables.
    /// </summary>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="variables">Variables accessible within the expression.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    public object? Evaluate(string expression, params object?[] variables)
    {
        if (variables.Length == 0)
            return Evaluate(expression, (IDictionary<string, object?>?)null);

        return Evaluate(expression, VariableBindingProjector.BuildPositionalVariables(variables));
    }

    /// <summary>
    /// Evaluates a C# expression with inline variables and converts the result to <typeparamref name="T"/>.
    /// Variables are accessible as <c>@0</c>, <c>@1</c>, etc. by position.
    /// Dictionaries and objects are also destructured into named variables.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="variables">Variables accessible within the expression.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public T? Evaluate<T>(string expression, params object?[] variables)
    {
        var result = Evaluate(expression, variables);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// Evaluates a pre-parsed expression with inline variables.
    /// Variables are accessible as <c>@0</c>, <c>@1</c>, etc. by position.
    /// Dictionaries and objects are also destructured into named variables.
    /// </summary>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">Variables accessible within the expression.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    public object? Evaluate(AlderExpression expression, params object?[] variables)
    {
        if (variables.Length == 0)
            return Evaluate(expression, (IDictionary<string, object?>?)null);

        return Evaluate(expression, VariableBindingProjector.BuildPositionalVariables(variables));
    }

    /// <summary>
    /// Evaluates a pre-parsed expression with inline variables and converts the result to <typeparamref name="T"/>.
    /// Variables are accessible as <c>@0</c>, <c>@1</c>, etc. by position.
    /// Dictionaries and objects are also destructured into named variables.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">Variables accessible within the expression.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public T? Evaluate<T>(AlderExpression expression, params object?[] variables)
    {
        var result = Evaluate(expression, variables);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// Attempts to evaluate a C# expression without throwing on failure.
    /// </summary>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="result">When successful, the evaluation result; otherwise, <c>null</c>.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns><c>true</c> if evaluation succeeded; otherwise, <c>false</c>.</returns>
    public bool TryEvaluate(
        string expression,
        out object? result,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        try
        {
            result = Evaluate(expression, variables, cancellationToken);
            return true;
        }
        catch (Exception ex) when (!ShouldRethrowTryApiException(ex))
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to evaluate a pre-parsed expression without throwing on failure.
    /// </summary>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="result">When successful, the evaluation result; otherwise, <c>null</c>.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns><c>true</c> if evaluation succeeded; otherwise, <c>false</c>.</returns>
    public bool TryEvaluate(
        AlderExpression expression,
        out object? result,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        try
        {
            result = Evaluate(expression, variables, cancellationToken);
            return true;
        }
        catch (Exception ex) when (!ShouldRethrowTryApiException(ex))
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to evaluate a C# expression and convert the result to <typeparamref name="T"/> without throwing on failure.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="result">When successful, the result converted to <typeparamref name="T"/>; otherwise, <c>default</c>.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns><c>true</c> if evaluation and conversion succeeded; otherwise, <c>false</c>.</returns>
    public bool TryEvaluate<T>(
        string expression,
        out T? result,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        try
        {
            result = Evaluate<T>(expression, variables, cancellationToken);
            return true;
        }
        catch (Exception ex) when (!ShouldRethrowTryApiException(ex))
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to evaluate a pre-parsed expression and convert the result to <typeparamref name="T"/> without throwing on failure.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="result">When successful, the result converted to <typeparamref name="T"/>; otherwise, <c>default</c>.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns><c>true</c> if evaluation and conversion succeeded; otherwise, <c>false</c>.</returns>
    public bool TryEvaluate<T>(
        AlderExpression expression,
        out T? result,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        try
        {
            result = Evaluate<T>(expression, variables, cancellationToken);
            return true;
        }
        catch (Exception ex) when (!ShouldRethrowTryApiException(ex))
        {
            result = default;
            return false;
        }
    }
}
