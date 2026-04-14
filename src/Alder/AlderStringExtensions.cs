namespace Alder;

/// <summary>
/// Convenience extension methods that route string-based evaluation through <see cref="AlderEval"/>.
/// </summary>
public static class AlderStringExtensions
{
    /// <summary>
    /// Evaluates this string and returns the result.
    /// </summary>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    public static object? Evaluate(
        this string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.Evaluate(expression, variables, cancellationToken);

    /// <summary>
    /// Evaluates this string using public properties from <paramref name="variables"/> as locals.
    /// </summary>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    public static object? Evaluate(
        this string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => AlderEval.Evaluate(expression, variables, cancellationToken);

    /// <summary>
    /// Evaluates this string and converts the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public static T? Evaluate<T>(
        this string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.Evaluate<T>(expression, variables, cancellationToken);

    /// <summary>
    /// Evaluates this string with object-backed locals and converts the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public static T? Evaluate<T>(
        this string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => AlderEval.Evaluate<T>(expression, variables, cancellationToken);

    /// <summary>
    /// Evaluates this string with positional variables.
    /// Positional variables are available as <c>@0</c>, <c>@1</c>, and so on.
    /// Dictionaries and complex objects are also projected into named variables.
    /// </summary>
    public static object? Evaluate(
        this string expression,
        params object?[] variables)
        => AlderEval.Evaluate(expression, variables);

    /// <summary>
    /// Evaluates this string with positional variables and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public static T? Evaluate<T>(
        this string expression,
        params object?[] variables)
        => AlderEval.Evaluate<T>(expression, variables);

    /// <summary>
    /// Asynchronously evaluates this string and returns the result.
    /// </summary>
    public static ValueTask<object?> EvaluateAsync(
        this string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.EvaluateAsync(expression, variables, cancellationToken);

    /// <summary>
    /// Asynchronously evaluates this string using public properties from <paramref name="variables"/> as locals.
    /// </summary>
    public static ValueTask<object?> EvaluateAsync(
        this string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => AlderEval.EvaluateAsync(expression, variables, cancellationToken);

    /// <summary>
    /// Asynchronously evaluates this string and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public static ValueTask<T?> EvaluateAsync<T>(
        this string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.EvaluateAsync<T>(expression, variables, cancellationToken);

    /// <summary>
    /// Asynchronously evaluates this string with object-backed locals and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public static ValueTask<T?> EvaluateAsync<T>(
        this string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => AlderEval.EvaluateAsync<T>(expression, variables, cancellationToken);

    /// <summary>
    /// Asynchronously evaluates this string with positional variables.
    /// </summary>
    public static ValueTask<object?> EvaluateAsync(
        this string expression,
        params object?[] variables)
        => AlderEval.EvaluateAsync(expression, variables);

    /// <summary>
    /// Asynchronously evaluates this string with positional variables and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public static ValueTask<T?> EvaluateAsync<T>(
        this string expression,
        params object?[] variables)
        => AlderEval.EvaluateAsync<T>(expression, variables);

    /// <summary>
    /// Attempts to evaluate this string without throwing for ordinary failures.
    /// </summary>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="result">When successful, the evaluation result; otherwise, <c>null</c>.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns><c>true</c> if evaluation succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryEvaluate(
        this string expression,
        out object? result,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.TryEvaluate(expression, out result, variables, cancellationToken);

    /// <summary>
    /// Attempts to evaluate this string and convert the result to <typeparamref name="T"/> without throwing for ordinary failures.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">Expression source to evaluate.</param>
    /// <param name="result">When successful, the result converted to <typeparamref name="T"/>; otherwise, <c>default</c>.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns><c>true</c> if evaluation and conversion succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryEvaluate<T>(
        this string expression,
        out T? result,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.TryEvaluate(expression, out result, variables, cancellationToken);
}
