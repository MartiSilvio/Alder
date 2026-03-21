using Alder.Runtime;

namespace Alder;

/// <summary>
/// Wraps a compiled expression delegate for repeated invocation without engine dispatch overhead.
/// The expression is compiled once via <see cref="AlderEngine.Compile{T}"/> and can then be
/// invoked millions of times via <see cref="Invoke(CancellationToken)"/> or
/// <see cref="Invoke(IDictionary{string, object?}, CancellationToken)"/>.
/// </summary>
/// <typeparam name="T">The expected return type of the expression.</typeparam>
public sealed class AlderCompiledExpression<T>
{
    private readonly CompiledExpressionDelegate _delegate;
    private readonly AlderEngine _engine;
    private readonly AlderOptions _options;

    internal AlderCompiledExpression(
        CompiledExpressionDelegate compiledDelegate,
        AlderEngine engine,
        AlderOptions options)
    {
        _delegate = compiledDelegate;
        _engine = engine;
        _options = options;
    }

    /// <summary>
    /// Invokes the compiled expression using the engine's current context.
    /// Variables set via <see cref="AlderEngine.SetVariable"/> after compilation
    /// are visible because the context is captured by reference.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>The evaluated result, converted to <typeparamref name="T"/>.</returns>
    public T? Invoke(CancellationToken cancellationToken = default)
    {
        var baseContext = _engine.GetContextForCompiled();
        var executionContext = PrepareExecutionContext(baseContext);
        var result = _delegate(executionContext, _options, cancellationToken);
        return ConvertResult(result);
    }

    /// <summary>
    /// Invokes the compiled expression with per-invocation variables.
    /// Creates a child context so the provided variables do not pollute the engine's shared state.
    /// </summary>
    /// <param name="variables">Variables available during this invocation only.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>The evaluated result, converted to <typeparamref name="T"/>.</returns>
    public T? Invoke(IDictionary<string, object?> variables, CancellationToken cancellationToken = default)
    {
        var parentContext = _engine.GetContextForCompiled();
        var childContext = parentContext.CreateChild();
        foreach (var (name, value) in variables)
        {
            childContext.Define(name, value);
        }
        var executionContext = PrepareExecutionContext(childContext);
        var result = _delegate(executionContext, _options, cancellationToken);
        return ConvertResult(result);
    }

    private AlderContext PrepareExecutionContext(AlderContext context)
    {
        if (_options.Constraints == null)
            return context;

        var executionContext = context.CreateChild();
        var state = new ExecutionConstraintState();
        state.Reset(_options.Constraints);
        executionContext.ConstraintState = state;
        return executionContext;
    }

    private static T? ConvertResult(object? result)
    {
        return result switch
        {
            null => default,
            T typed => typed,
            _ => (T)Convert.ChangeType(result, typeof(T))
        };
    }
}
