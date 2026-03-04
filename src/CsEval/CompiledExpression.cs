using CsEval.Runtime;

namespace CsEval;

/// <summary>
/// Wraps a compiled expression delegate for repeated invocation without engine dispatch overhead.
/// The expression is compiled once via <see cref="CsEvalEngine.Compile{T}"/> and can then be
/// invoked millions of times via <see cref="Invoke(CancellationToken)"/> or
/// <see cref="Invoke(IDictionary{string, object?}, CancellationToken)"/>.
/// </summary>
/// <typeparam name="T">The expected return type of the expression.</typeparam>
public sealed class CsEvalCompiledExpression<T>
{
    private readonly CompiledExpressionDelegate _delegate;
    private readonly CsEvalEngine _engine;
    private readonly CsEvalOptions _options;
    private readonly Func<MethodInfo, object?[], object?[]>? _argumentTransformer;

    internal CsEvalCompiledExpression(
        CompiledExpressionDelegate compiledDelegate,
        CsEvalEngine engine,
        CsEvalOptions options,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer)
    {
        _delegate = compiledDelegate;
        _engine = engine;
        _options = options;
        _argumentTransformer = argumentTransformer;
    }

    /// <summary>
    /// Invokes the compiled expression using the engine's current context.
    /// Variables set via <see cref="CsEvalEngine.SetVariable"/> after compilation
    /// are visible because the context is captured by reference.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>The evaluated result, converted to <typeparamref name="T"/>.</returns>
    public T? Invoke(CancellationToken cancellationToken = default)
    {
        var context = _engine.GetContextForCompiled();
        var result = _delegate(context, _options, cancellationToken, _argumentTransformer);
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
        var result = _delegate(childContext, _options, cancellationToken, _argumentTransformer);
        return ConvertResult(result);
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
