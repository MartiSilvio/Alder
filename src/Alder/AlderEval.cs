namespace Alder;

/// <summary>
/// Static entry point for expression evaluation using a shared global engine.
/// Configure once at startup via <see cref="Configure"/>, then evaluate from anywhere.
/// For per-engine configuration, use <see cref="AlderEngine"/> directly.
/// </summary>
public static class AlderEval
{
    private static volatile AlderEngine? _engine;
    private static Action<AlderOptions>? _pendingConfigure;
    private static readonly object _lock = new();

    private enum State { Unconfigured, Configured, EngineCreated }
    private static State _state;

    /// <summary>
    /// Configures the global engine options. Must be called before any evaluation.
    /// Can only be called once. Thread-safe.
    /// </summary>
    /// <param name="configure">An action that configures the global engine options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Configuration has already been set, or evaluation has already started.</exception>
    public static void Configure(Action<AlderOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        lock (_lock)
        {
            switch (_state)
            {
                case State.EngineCreated:
                    throw new InvalidOperationException(
                        "Cannot configure AlderEval after evaluation has started. " +
                        "Call AlderEval.Configure() before the first AlderEval.Evaluate() call.");
                case State.Configured:
                    throw new InvalidOperationException(
                        "AlderEval.Configure() has already been called. " +
                        "Global configuration can only be set once.");
                default:
                    _pendingConfigure = configure;
                    _state = State.Configured;
                    break;
            }
        }
    }

    /// <summary>
    /// Resets the global engine, clearing any configuration and cached state.
    /// Primarily intended for testing. Not safe to call while evaluations are in flight.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _engine?.Dispose();
            _engine = null;
            _pendingConfigure = null;
            _state = State.Unconfigured;
        }
    }

    internal static AlderEngine GetEngine()
    {
        var engine = _engine;
        if (engine != null)
            return engine;

        lock (_lock)
        {
            if (_engine != null)
                return _engine;

            _engine = _pendingConfigure != null
                ? new AlderEngine(_pendingConfigure)
                : new AlderEngine();

            _state = State.EngineCreated;
            return _engine;
        }
    }

    /// <summary>
    /// Evaluates a C# expression string and returns the result.
    /// </summary>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    public static object? Evaluate(
        string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => GetEngine().Evaluate(expression, variables, cancellationToken: cancellationToken);

    /// <summary>
    /// Evaluates a C# expression with variables supplied as an anonymous object.
    /// </summary>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    public static object? Evaluate(
        string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => GetEngine().Evaluate(expression, variables, cancellationToken: cancellationToken);

    /// <summary>
    /// Evaluates a C# expression and converts the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public static T? Evaluate<T>(
        string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => GetEngine().Evaluate<T>(expression, variables, cancellationToken: cancellationToken);

    /// <summary>
    /// Evaluates a C# expression with anonymous object variables and converts the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public static T? Evaluate<T>(
        string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => GetEngine().Evaluate<T>(expression, variables, cancellationToken: cancellationToken);

    /// <summary>
    /// Evaluates a C# expression with inline variables.
    /// Variables are accessible as <c>@0</c>, <c>@1</c>, etc. by position.
    /// Dictionaries and objects are also destructured into named variables.
    /// </summary>
    public static object? Evaluate(string expression, params object?[] variables)
        => GetEngine().Evaluate(expression, variables);

    /// <summary>
    /// Evaluates a C# expression with inline variables and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public static T? Evaluate<T>(string expression, params object?[] variables)
        => GetEngine().Evaluate<T>(expression, variables);

    /// <summary>
    /// Attempts to evaluate a C# expression without throwing on failure.
    /// </summary>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="result">When successful, the evaluation result; otherwise, <c>null</c>.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns><c>true</c> if evaluation succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryEvaluate(
        string expression,
        out object? result,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => GetEngine().TryEvaluate(expression, out result, variables, cancellationToken: cancellationToken);

    /// <summary>
    /// Attempts to evaluate a C# expression and convert the result to <typeparamref name="T"/> without throwing on failure.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="result">When successful, the result converted to <typeparamref name="T"/>; otherwise, <c>default</c>.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns><c>true</c> if evaluation and conversion succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryEvaluate<T>(
        string expression,
        out T? result,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => GetEngine().TryEvaluate(expression, out result, variables, cancellationToken: cancellationToken);

    /// <summary>
    /// Asynchronously evaluates a C# expression string and returns the result.
    /// Required for expressions containing <c>await</c>. Also works for non-async expressions.
    /// </summary>
    public static ValueTask<object?> EvaluateAsync(
        string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => GetEngine().EvaluateAsync(expression, variables, cancellationToken);

    /// <summary>
    /// Asynchronously evaluates a C# expression and converts the result to <typeparamref name="T"/>.
    /// </summary>
    public static ValueTask<T?> EvaluateAsync<T>(
        string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
        => GetEngine().EvaluateAsync<T>(expression, variables, cancellationToken);

    /// <summary>
    /// Validates an expression for syntax and binding errors without evaluating it.
    /// </summary>
    /// <param name="expression">The C# expression string to validate.</param>
    /// <param name="diagnostics">When validation fails, the list of diagnostics; otherwise, an empty list.</param>
    /// <returns><c>true</c> if the expression is valid; otherwise, <c>false</c>.</returns>
    public static bool TryValidate(
        string expression,
        out IReadOnlyList<AlderDiagnostic> diagnostics)
        => GetEngine().TryValidate(expression, out diagnostics);
}
