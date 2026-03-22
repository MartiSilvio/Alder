namespace Alder;

/// <summary>
/// String extension methods for evaluating C# expressions.
/// Uses the global <see cref="AlderEval"/> engine.
/// </summary>
public static class AlderStringExtensions
{
    public static object? Evaluate(
        this string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.Evaluate(expression, variables, cancellationToken);

    public static object? Evaluate(
        this string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => AlderEval.Evaluate(expression, variables, cancellationToken);

    public static T? Evaluate<T>(
        this string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.Evaluate<T>(expression, variables, cancellationToken);

    public static T? Evaluate<T>(
        this string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => AlderEval.Evaluate<T>(expression, variables, cancellationToken);

    public static bool TryEvaluate(
        this string expression,
        out object? result,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.TryEvaluate(expression, out result, variables, cancellationToken);

    public static bool TryEvaluate<T>(
        this string expression,
        out T? result,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => AlderEval.TryEvaluate(expression, out result, variables, cancellationToken);
}
