using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Alder.Diagnostics;
using Alder.Binding;
using Alder.Compilation;
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

        if (expression.CompiledInfo != null)
            return expression.CompiledInfo.Delegate != null;

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
            var reason = expression.CompiledInfo?.FailureReason ?? "No compiler configured. Add Alder.Compiled and call UseCompiler() on options.";
            throw new AlderException(
                DiagnosticDescriptors.StrictCompilationFailed,
                $"Cannot compile expression '{expression.Source}': {reason}");
        }
    }

    private bool TryCompileInternal(AlderExpression expression, AlderContext context)
    {
        var compiler = _config.Compiler!;

        var existing = expression.CompiledInfo;
        if (existing != null)
            return existing.Delegate != null;

        lock (expression)
        {
            existing = expression.CompiledInfo;
            if (existing != null)
                return existing.Delegate != null;

            if (!expression.TryGetOrCreateBoundExpression(context, _config.Constraints.MaxExpressionDepth, out var bound, out var failureReason) ||
                bound == null)
            {
                expression.CompiledInfo = new CompiledExpressionInfo(null, false, failureReason ?? "Binding failed for expression.");
                return false;
            }

            bound = RunCompilationPipeline(bound);
            expression.CompiledInfo = compiler.TryCompile(bound, _config);
            return expression.CompiledInfo.Delegate != null;
        }
    }

    /// <summary>
    /// Attempts to compile the expression using the AST path (no binding context needed).
    /// Used when no context is available.
    /// </summary>
    internal bool TryCompileFromAst(AlderExpression expression)
    {
        var compiler = _config.Compiler;
        if (compiler == null)
            return false;

        if (expression.CompiledInfo != null)
            return expression.CompiledInfo.Delegate != null;

        expression.CompiledInfo = expression._expressionCache != null
            ? compiler.GetOrCompile(expression.Source, expression.Ast, expression._expressionCache, _config)
            : compiler.TryCompile(expression.Ast, _config);
        return expression.CompiledInfo.Delegate != null;
    }

    private object? ExecuteCompiledExpression(
        AlderExpression expression,
        AlderContext compileContext,
        AlderContext executionContext,
        ExecutionConstraintState constraintState,
        CancellationToken cancellationToken)
    {
        var compiled = expression.GetCompiledInfo();
        if (compiled == null)
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
    }

    /// <summary>
    /// Exposes the engine's evaluation context for use by <see cref="AlderCompiledExpression{T}"/>.
    /// The context is captured by reference so that variable changes after compilation are visible.
    /// </summary>
    internal AlderContext GetContextForCompiled() => GetOrCreateContext();
}
