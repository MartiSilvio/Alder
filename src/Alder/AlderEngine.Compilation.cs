using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder;

public sealed partial class AlderEngine
{

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
            var reason = expression.CompiledInfo?.FailureReason ?? "No compiler configured. Call UseCompiler() on options.";
            throw new AlderException(
                DiagnosticDescriptors.StrictCompilationFailed,
                $"Cannot compile expression '{expression.Source}': {reason}");
        }
    }

    private bool TryCompileInternal(AlderExpression expression, AlderContext context)
    {
        var compiler = _config.Compiler!;

        if (IsCompiledInfoCurrent(expression.CompiledInfo, context))
            return expression.CompiledInfo!.Delegate != null;

        lock (expression)
        {
            if (IsCompiledInfoCurrent(expression.CompiledInfo, context))
                return expression.CompiledInfo!.Delegate != null;

            var version = context.GetTypeInferenceVersion();

            if (!expression.TryGetOrCreateBoundExpression(context, out var bound, out var failureReason) ||
                bound == null)
            {
                expression.CompiledInfo = new CompiledExpressionInfo(null, false,
                    failureReason ?? "Binding failed for expression.", TypeVersion: version);
                return false;
            }

            bound = RunCompilationPipeline(bound);
            var compiled = compiler.TryCompile(bound, _config);
            expression.CompiledInfo = compiled with { TypeVersion = version };
            return expression.CompiledInfo.Delegate != null;
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
        var compiled = expression.GetCompiledInfo();
        if (!IsCompiledInfoCurrent(compiled, compileContext))
        {
            TryCompileInternal(expression, compileContext);
            compiled = expression.GetCompiledInfo();
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
        internal void ThrowIfDisposed() => _engine.ThrowIfDisposed();
        internal CompiledExpressionInfo CompileWithAdditionalVariables(
            AlderExpression expression,
            IReadOnlyDictionary<string, Type> additionalVariableTypes)
            => _engine.CompileWithAdditionalVariables(expression, additionalVariableTypes);
    }

    /// <summary>
    /// Exposes the engine's evaluation context for use by <see cref="AlderCompiledExpression{T}"/>.
    /// The context is captured by reference so that variable changes after compilation are visible.
    /// </summary>
    internal AlderContext GetContextForCompiled() => GetOrCreateContext();

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

        if (!expression.TryGetOrCreateBoundExpression(bindingContext, out var bound, out var failureReason) ||
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
}
