namespace Alder;

/// <summary>
/// String extension methods for evaluating C# expressions.
/// Uses the global <see cref="AlderEval"/> engine.
/// </summary>
public static class AlderStringExtensions
{
    /// <summary>
    /// Evaluates this string as a C# expression and returns the result.
    /// </summary>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    public static object? Evaluate(
        this string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.Evaluate(expression, variables, cancellationToken);

    /// <summary>
    /// Evaluates this string as a C# expression with variables supplied as an anonymous object.
    /// </summary>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    public static object? Evaluate(
        this string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => AlderEval.Evaluate(expression, variables, cancellationToken);

    /// <summary>
    /// Evaluates this string as a C# expression and converts the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public static T? Evaluate<T>(
        this string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.Evaluate<T>(expression, variables, cancellationToken);

    /// <summary>
    /// Evaluates this string as a C# expression with anonymous object variables and converts the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public static T? Evaluate<T>(
        this string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => AlderEval.Evaluate<T>(expression, variables, cancellationToken);

    /// <summary>
    /// Evaluates this string as a C# expression with inline variables.
    /// Variables are accessible as <c>@0</c>, <c>@1</c>, etc. by position.
    /// Dictionaries and objects are also destructured into named variables.
    /// </summary>
    public static object? Evaluate(
        this string expression,
        params object?[] variables)
        => AlderEval.Evaluate(expression, variables);

    /// <summary>
    /// Evaluates this string as a C# expression with inline variables and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public static T? Evaluate<T>(
        this string expression,
        params object?[] variables)
        => AlderEval.Evaluate<T>(expression, variables);

    /// <summary>
    /// Attempts to evaluate this string as a C# expression without throwing on failure.
    /// </summary>
    /// <param name="expression">The C# expression string to evaluate.</param>
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
    /// Attempts to evaluate this string as a C# expression and convert the result to <typeparamref name="T"/> without throwing on failure.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The C# expression string to evaluate.</param>
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
