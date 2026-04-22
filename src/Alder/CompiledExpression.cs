using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder;

/// <summary>
/// Wraps a compiled expression delegate for repeated invocation against an <see cref="AlderEngine"/>.
/// </summary>
/// <typeparam name="T">The expected return type of the expression.</typeparam>
public sealed class AlderCompiledExpression<T>
{
    private readonly CompiledExpressionDelegate _delegate;
    private readonly AlderEngine _engine;
    private readonly AlderConfig _config;
    private readonly int _compiledTypeVersion;

    internal AlderCompiledExpression(
        CompiledExpressionDelegate compiledDelegate,
        AlderEngine engine,
        AlderConfig config,
        int compiledTypeVersion)
    {
        _delegate = compiledDelegate;
        _engine = engine;
        _config = config;
        _compiledTypeVersion = compiledTypeVersion;
    }

    /// <summary>
    /// Invokes the compiled expression against the engine's current context.
    /// Variables added after compilation stay visible because the delegate closes over the engine context.
    /// Changing a visible variable's static type invalidates the compiled delegate.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>The evaluated result, converted to <typeparamref name="T"/>.</returns>
    /// <exception cref="AlderException">Thrown when a variable's type has changed since compilation (ALDR0003).</exception>
    public T? Invoke(CancellationToken cancellationToken = default)
    {
        return InvokeCore(null, cancellationToken);
    }

    /// <summary>
    /// Invokes the compiled expression with per-call variables.
    /// The variables are applied to a child context so the engine's shared scope stays unchanged.
    /// </summary>
    /// <param name="variables">Variables available during this invocation only.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>The evaluated result, converted to <typeparamref name="T"/>.</returns>
    /// <exception cref="AlderException">Thrown when a variable's type has changed since compilation (ALDR0003).</exception>
    public T? Invoke(IDictionary<string, object?> variables, CancellationToken cancellationToken = default)
    {
        return InvokeCore(variables, cancellationToken);
    }

    private T? InvokeCore(IDictionary<string, object?>? variables, CancellationToken cancellationToken)
    {
        using var state = _engine.CreateCompiledInvocationState(_compiledTypeVersion, cancellationToken);
        if (variables != null)
        {
            foreach (var (name, value) in variables)
            {
                state.ExecutionContext.Define(name, value);
            }
        }

        var result = _delegate(state.ExecutionContext, _config, state.ConstraintState, cancellationToken);
        return AlderTypedResultConverter.Convert<T>(result);
    }
}
