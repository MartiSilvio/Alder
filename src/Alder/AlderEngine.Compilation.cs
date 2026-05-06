using System.Collections.Concurrent;
using Alder.Binding;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder;

public sealed partial class AlderEngine
{
    private static readonly ConcurrentBag<ExecutionConstraintState> ConstraintStatePool = new();

    /// <summary>
    /// Attempts to compile the expression to IL. Returns <c>true</c> if successful.
    /// Requires a compiler to be configured via <c>UseCompiler()</c> on options.
    /// </summary>
    /// <param name="expression">The parsed expression to compile.</param>
    /// <returns><c>true</c> if compilation succeeded; <c>false</c> if no compiler is configured or compilation fails.</returns>
    /// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
    public bool TryCompile(AlderExpression expression)
    {
        ThrowIfDisposed();
        if (_config.Compiler == null)
            return false;

        var context = GetOrCreateContext();
        return TryCompileInternal(expression, context);
    }

    /// <summary>
    /// Compiles the expression to IL. Throws if compilation fails or no compiler is configured.
    /// </summary>
    /// <param name="expression">The parsed expression to compile.</param>
    /// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
    /// <exception cref="AlderException">Compilation fails or no compiler is configured.</exception>
    public void Compile(AlderExpression expression)
    {
        ThrowIfDisposed();
        if (!TryCompile(expression))
        {
            var reason = GetCompilationFailureReason(expression) ?? "No compiler configured. Call UseCompiler() on options.";
            throw new AlderException(
                DiagnosticDescriptors.StrictCompilationFailed,
                $"Cannot compile expression '{expression.Source}': {reason}");
        }
    }

    private bool TryCompileInternal(AlderExpression expression, AlderContext context)
    {
        var compiler = _config.Compiler!;
        var state = GetExpressionState(expression);

        if (IsCompiledInfoCurrent(GetCompiledInfo(expression), context))
            return HasCompiledDelegate(expression);

        lock (state)
        {
            if (IsCompiledInfoCurrent(GetCompiledInfo(expression), context))
                return HasCompiledDelegate(expression);

            var version = context.GetTypeInferenceVersion();

            Alder.Binding.BoundExpr? bound;
            string? failureReason;
            try
            {
                if (!TryGetOrCreateBoundExpression(expression, context, out bound, out failureReason) ||
                    bound == null)
                {
                    SetCompiledInfo(expression, new CompiledExpressionInfo(
                        null,
                        false,
                        failureReason ?? "Binding failed for expression.",
                        TypeVersion: version));
                    return false;
                }
            }
            catch (AlderException ex)
            {
                // Bind-time semantic errors (CS0163 fall-through, etc.) mean the expression cannot
                // be compiled. Surface them as a compile failure rather than propagating — TryCompile
                // is a probe, not an assertion.
                SetCompiledInfo(expression, new CompiledExpressionInfo(null, false, ex.Message, ex, TypeVersion: version));
                return false;
            }

            bound = RunCompilationPipeline(bound);
            var compiled = compiler.TryCompile(bound, _config);
            SetCompiledInfo(expression, compiled with { TypeVersion = version });
            return HasCompiledDelegate(expression);
        }
    }

    private static bool IsCompiledInfoCurrent(CompiledExpressionInfo? info, AlderContext context)
    {
        if (info == null) return false;
        if (info.TypeVersion == null) return true;
        return info.TypeVersion == context.GetTypeInferenceVersion();
    }

    private object? ExecuteCompiledExpression(
        AlderExpression expression,
        AlderContext compileContext,
        AlderContext executionContext,
        ExecutionConstraintState constraintState,
        CancellationToken cancellationToken)
    {
        var compiled = GetCompiledInfo(expression);
        if (!IsCompiledInfoCurrent(compiled, compileContext))
        {
            TryCompileInternal(expression, compileContext);
            compiled = GetCompiledInfo(expression);
        }

        if (compiled?.Delegate != null)
        {
            try
            {
                return compiled.Delegate(executionContext, _config, constraintState, cancellationToken);
            }
            catch (AlderException ex) when (ex.Span.IsEmpty && !expression.Ast.Span.IsEmpty)
            {
                EnrichCompiledExceptionDiagnostics(ex, expression);
                throw;
            }
        }

        if (compiled?.FailureException is AlderException alderFailure)
            throw alderFailure;

        var reason = compiled?.FailureReason ?? "Unknown compilation failure";
        throw new AlderException(DiagnosticDescriptors.StrictCompilationFailed, reason);
    }

    private static void EnrichCompiledExceptionDiagnostics(AlderException ex, AlderExpression expression)
    {
        var sourceText = new Text.SourceText(expression.Source);
        var pos = sourceText.GetLinePosition(expression.Ast.Span.Start);
        ex.EnrichDiagnosticsWithPosition(expression.Ast.Span, pos.Line + 1, pos.Character + 1);
    }

    internal CompiledFeatureAccess GetCompiledFeatureAccess() => new(this);

    internal sealed class CompiledFeatureAccess
    {
        private readonly AlderEngine _engine;

        internal CompiledFeatureAccess(AlderEngine engine)
        {
            _engine = engine;
        }

        internal AlderConfig Config => _engine._config;
        internal AlderContext GetOrCreateContext() => _engine.GetOrCreateContext();
        internal Dictionary<string, object?> CollectEngineVariables() => _engine.CollectEngineVariables();
        internal BoundExpr RunCompilationPipeline(BoundExpr tree) => _engine.RunCompilationPipeline(tree);
        internal void ThrowIfDisposed() => _engine.ThrowIfDisposed();
        internal CompiledExpressionInfo? GetCompiledInfo(AlderExpression expression) => _engine.GetCompiledInfo(expression);
        internal string? GetCompilationFailureReason(AlderExpression expression) => _engine.GetCompilationFailureReason(expression);
        internal CompiledExpressionInfo CompileWithAdditionalVariables(
            AlderExpression expression,
            IReadOnlyDictionary<string, Type> additionalVariableTypes)
            => _engine.CompileWithAdditionalVariables(expression, additionalVariableTypes);
    }

    /// <summary>
    /// Exposes the engine's evaluation context for use by <see cref="AlderCompiledExpression{T}"/>.
    /// The context is captured by reference so that variable changes after compilation are visible.
    /// </summary>
    internal AlderContext GetContextForCompiled()
    {
        ThrowIfDisposed();
        return GetOrCreateContext();
    }

    internal AlderContext CreateCompiledInvocationContext(int expectedTypeVersion, CancellationToken cancellationToken)
    {
        var parentContext = GetContextForCompiled();
        if (parentContext.GetTypeInferenceVersion() != expectedTypeVersion)
            throw new AlderException(DiagnosticDescriptors.CompiledExpressionStale);

        var executionContext = parentContext.CreateChild();
        executionContext.ActiveCancellationToken = cancellationToken;
        return executionContext;
    }

    internal ExecutionConstraintState RentExecutionConstraintState()
    {
        if (!ConstraintStatePool.TryTake(out var constraintState))
            constraintState = new ExecutionConstraintState();
        constraintState.Reset(_config.Constraints);
        return constraintState;
    }

    internal static void ReturnExecutionConstraintState(ExecutionConstraintState constraintState)
    {
        constraintState.Reset(null);
        ConstraintStatePool.Add(constraintState);
    }

    private CompiledExpressionInfo CompileWithAdditionalVariables(
        AlderExpression expression,
        IReadOnlyDictionary<string, Type> additionalVariableTypes)
    {
        ThrowIfDisposed();

        if (_config.Compiler == null)
        {
            return new CompiledExpressionInfo(
                null,
                false,
                "No compiler configured. Call UseCompiler() on options.");
        }

        var bindingContext = CreateBindingContext(additionalVariableTypes);
        var version = bindingContext.GetTypeInferenceVersion();

        if (!TryGetOrCreateBoundExpression(expression, bindingContext, out var bound, out var failureReason) ||
            bound == null)
        {
            return new CompiledExpressionInfo(
                null,
                false,
                failureReason ?? "Binding failed for expression.",
                TypeVersion: version);
        }

        bound = RunCompilationPipeline(bound);
        return _config.Compiler.TryCompile(bound, _config) with { TypeVersion = version };
    }

    private AlderContext CreateBindingContext(IReadOnlyDictionary<string, Type> additionalVariableTypes)
    {
        var bindingContext = new AlderContext(_config, _config.ServiceProvider);

        foreach (var (name, value) in CollectEngineVariables())
            bindingContext.Define(name, value, value?.GetType() ?? typeof(object));

        foreach (var (name, type) in additionalVariableTypes)
            bindingContext.Define(name, null, type);

        return bindingContext;
    }

    private Dictionary<string, object?> CollectEngineVariables()
    {
        var variables = new Dictionary<string, object?>(_config.Comparer);

        lock (_contextInitLock)
        {
            foreach (var (name, pending) in _pendingVariables)
                variables[name] = pending.Value;
        }

        if (_context != null)
        {
            foreach (var (name, value) in _context.GetAllVisible())
                variables[name] = value;
        }

        return variables;
    }
}
