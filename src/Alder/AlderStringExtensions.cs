namespace Alder;

/// <summary>
/// Convenience extension methods that route string-based evaluation through <see cref="AlderEval"/>.
/// </summary>
public static class AlderStringExtensions
{
    /// <summary>
    /// Evaluates this string and returns the result.
    /// </summary>
    public static object? Evaluate(
        this string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.GetEngine().Evaluate(expression, variables, cancellationToken);

    /// <summary>
    /// Evaluates this string using public properties from <paramref name="variables"/> as locals.
    /// </summary>
    public static object? Evaluate(
        this string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => AlderEval.GetEngine().Evaluate(expression, variables, cancellationToken);

    /// <summary>
    /// Evaluates this string and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public static T? Evaluate<T>(
        this string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.GetEngine().Evaluate<T>(expression, variables, cancellationToken);

    /// <summary>
    /// Evaluates this string with object-backed locals and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public static T? Evaluate<T>(
        this string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => AlderEval.GetEngine().Evaluate<T>(expression, variables, cancellationToken);

    /// <summary>
    /// Evaluates this string with positional variables.
    /// Positional variables are available as <c>@0</c>, <c>@1</c>, and so on.
    /// Dictionaries and complex objects are also projected into named variables.
    /// </summary>
    public static object? Evaluate(
        this string expression,
        params object?[] variables)
        => AlderEval.GetEngine().Evaluate(expression, variables);

    /// <summary>
    /// Evaluates this string with positional variables and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public static T? Evaluate<T>(
        this string expression,
        params object?[] variables)
        => AlderEval.GetEngine().Evaluate<T>(expression, variables);

    /// <summary>
    /// Asynchronously evaluates this string and returns the result.
    /// </summary>
    public static ValueTask<object?> EvaluateAsync(
        this string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.GetEngine().EvaluateAsync(expression, variables, cancellationToken);

    /// <summary>
    /// Asynchronously evaluates this string using public properties from <paramref name="variables"/> as locals.
    /// </summary>
    public static ValueTask<object?> EvaluateAsync(
        this string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => AlderEval.GetEngine().EvaluateAsync(expression, variables, cancellationToken);

    /// <summary>
    /// Asynchronously evaluates this string and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public static ValueTask<T?> EvaluateAsync<T>(
        this string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.GetEngine().EvaluateAsync<T>(expression, variables, cancellationToken);

    /// <summary>
    /// Asynchronously evaluates this string with object-backed locals and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public static ValueTask<T?> EvaluateAsync<T>(
        this string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => AlderEval.GetEngine().EvaluateAsync<T>(expression, variables, cancellationToken);

    /// <summary>
    /// Asynchronously evaluates this string with positional variables.
    /// </summary>
    public static ValueTask<object?> EvaluateAsync(
        this string expression,
        params object?[] variables)
        => AlderEval.GetEngine().EvaluateAsync(expression, variables);

    /// <summary>
    /// Asynchronously evaluates this string with positional variables and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public static ValueTask<T?> EvaluateAsync<T>(
        this string expression,
        params object?[] variables)
        => AlderEval.GetEngine().EvaluateAsync<T>(expression, variables);

    /// <summary>
    /// Attempts to evaluate this string without throwing for ordinary failures.
    /// </summary>
    public static bool TryEvaluate(
        this string expression,
        out object? result,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.GetEngine().TryEvaluate(expression, out result, variables, cancellationToken);

    /// <summary>
    /// Attempts to evaluate this string and convert the result to <typeparamref name="T"/> without throwing for ordinary failures.
    /// </summary>
    public static bool TryEvaluate<T>(
        this string expression,
        out T? result,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.GetEngine().TryEvaluate(expression, out result, variables, cancellationToken);
}
