using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CsEval.Binding;
using CsEval.Compilation;
using CsEval.Diagnostics;
using CsEval.Interpretation;
using CsEval.Runtime;
using CsEval.Tracing;

namespace CsEval;

public sealed partial class CsEvalEngine
{
    private sealed record CompiledNoCancellationFastPath(
        CsEvalExpression Expression,
        CompiledExpressionFastDelegate Delegate,
        CsEvalContext Context);

    private volatile CompiledNoCancellationFastPath? _compiledNoCancellationFastPath;

    /// <summary>
    /// Attempts to compile the expression to IL. Returns true if successful.
    /// Requires a compiler to be configured via UseCompiler() on options.
    /// </summary>
    public bool TryCompile(CsEvalExpression expression)
    {
        ThrowIfDisposed();
        if (_options.Compiler == null)
            return false;

        if (expression.CompiledInfo != null)
            return expression.CompiledInfo.Delegate != null;

        var context = GetOrCreateContext(null);
        return TryCompileInternal(expression, context);
    }

    /// <summary>
    /// Compiles the expression to IL. Throws if compilation fails or no compiler is configured.
    /// </summary>
    public void Compile(CsEvalExpression expression)
    {
        ThrowIfDisposed();
        if (!TryCompile(expression))
        {
            var reason = expression.CompiledInfo?.FailureReason ?? "No compiler configured. Add CsEval.Compiled and call UseCompiler() on options.";
            throw new CsEvalException(
                DiagnosticDescriptors.StrictCompilationFailed,
                $"Cannot compile expression '{expression.Source}': {reason}");
        }
    }

    private bool TryCompileInternal(CsEvalExpression expression, CsEvalContext context)
    {
        var compiler = _options.Compiler!;

        var existing = expression.CompiledInfo;
        if (existing != null)
            return existing.Delegate != null;

        lock (expression)
        {
            existing = expression.CompiledInfo;
            if (existing != null)
                return existing.Delegate != null;

            if (!expression.TryGetOrCreateBoundExpression(context, _options.MaxExpressionDepth, out var bound, out var failureReason) ||
                bound == null)
            {
                expression.CompiledInfo = new CompiledExpressionInfo(null, false, failureReason ?? "Binding failed for expression.");
                return false;
            }

            expression.CompiledInfo = compiler.TryCompile(bound, _options);
            return expression.CompiledInfo.Delegate != null;
        }
    }

    /// <summary>
    /// Attempts to compile the expression using the AST path (no binding context needed).
    /// Used when no context is available.
    /// </summary>
    internal bool TryCompileFromAst(CsEvalExpression expression)
    {
        var compiler = _options.Compiler;
        if (compiler == null)
            return false;

        if (expression.CompiledInfo != null)
            return expression.CompiledInfo.Delegate != null;

        expression.CompiledInfo = expression._expressionCache != null
            ? compiler.GetOrCompile(expression.Source, expression.Ast, expression._expressionCache, _options)
            : compiler.TryCompile(expression.Ast, _options);
        return expression.CompiledInfo.Delegate != null;
    }

    public EvaluationTraceResult EvaluateWithTrace(
        string expression,
        IDictionary<string, object?>? variables = null,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var parsed = Parse(expression);
        return EvaluateWithTrace(parsed, variables, serviceProvider, cancellationToken);
    }

    public EvaluationTraceResult EvaluateWithTrace(
        CsEvalExpression expression,
        IDictionary<string, object?>? variables = null,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var target = this;
        if (variables != null)
        {
            target = CreateChild();
            target.SetVariables(variables);
        }

        var context = target.GetOrCreateContext(serviceProvider);
        var executionContext = context;

        var constraints = _options.Constraints;
        if (constraints != null)
        {
            executionContext = context.CreateChild();
            var state = new ExecutionConstraintState();
            state.Reset(constraints);
            executionContext.ConstraintState = state;
        }

        if (!expression.TryGetOrCreateBoundExpression(executionContext, _options.MaxExpressionDepth, out var boundExpression, out var failureReason) ||
            boundExpression == null)
        {
            expression.RecordBoundFallback(failureReason);
            if (IsDepthFailure(failureReason))
                throw new CsEvalDepthException("binding", _options.MaxExpressionDepth);
            throw new CsEvalException(DiagnosticDescriptors.BindingFailed, failureReason ?? "Binding failed for expression.");
        }

        var steps = new List<EvaluationTraceStep>();
        var evaluator = new BoundEvaluator(executionContext, _options, cancellationToken, steps, new Text.SourceText(expression.Source));
        var result = evaluator.Evaluate(boundExpression);
        expression.RecordBoundExecution();
        return new EvaluationTraceResult(UnwrapControlFlowSignal(result), steps);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryEvaluateCompiledNoCancellationCached(CsEvalExpression expression, out object? result)
    {
        var cached = _compiledNoCancellationFastPath;
        if (cached != null && ReferenceEquals(cached.Expression, expression))
        {
            try
            {
                result = cached.Delegate(cached.Context);
                return true;
            }
            catch (CsEvalException ex) when (ex.Span.IsEmpty && !expression.Ast.Span.IsEmpty)
            {
                EnrichCompiledExceptionDiagnostics(ex, expression);
                throw;
            }
        }

        result = null;
        return false;
    }

    private bool TryGetCompiledNoCancellationFastDelegate(
        CsEvalExpression expression,
        CsEvalContext context,
        [NotNullWhen(true)] out CompiledExpressionFastDelegate? fastDelegate)
    {
        var compiled = expression.GetCompiledInfo();
        if (compiled == null)
        {
            TryCompileInternal(expression, context);
            compiled = expression.GetCompiledInfo();
        }

        if (compiled?.FastDelegate is { } candidate &&
            ReferenceEquals(compiled.FastDelegateOptions, _options))
        {
            fastDelegate = candidate;
            return true;
        }

        fastDelegate = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CacheCompiledNoCancellationFastPath(
        CsEvalExpression expression,
        CompiledExpressionFastDelegate fastDelegate,
        CsEvalContext context)
    {
        _compiledNoCancellationFastPath = new CompiledNoCancellationFastPath(expression, fastDelegate, context);
    }

    private object? ExecuteCompiledExpression(
        CsEvalExpression expression,
        CsEvalContext compileContext,
        CsEvalContext executionContext,
        CancellationToken cancellationToken,
        bool allowNoCancellationFastPath)
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
                if (allowNoCancellationFastPath)
                {
                    if (compiled.FastDelegate is { } fastDelegate &&
                        ReferenceEquals(compiled.FastDelegateOptions, _options))
                    {
                        return fastDelegate(executionContext);
                    }

                    return compiled.Delegate(executionContext, _options, default);
                }

                return compiled.Delegate(executionContext, _options, cancellationToken);
            }
            catch (CsEvalException ex) when (ex.Span.IsEmpty && !expression.Ast.Span.IsEmpty)
            {
                EnrichCompiledExceptionDiagnostics(ex, expression);
                throw;
            }
        }

        if (compiled?.FailureException is CsEvalException csEvalFailure)
            throw csEvalFailure;

        var reason = compiled?.FailureReason ?? "Unknown compilation failure";
        throw new CsEvalException(DiagnosticDescriptors.StrictCompilationFailed, reason);
    }

    private static void EnrichCompiledExceptionDiagnostics(CsEvalException ex, CsEvalExpression expression)
    {
        var sourceText = new Text.SourceText(expression.Source);
        var pos = sourceText.GetLinePosition(expression.Ast.Span.Start);
        ex.EnrichDiagnosticsWithPosition(expression.Ast.Span, pos.Line + 1, pos.Character + 1);
    }

    internal CompiledFeatureAccess GetCompiledFeatureAccess() => new(this);

    internal sealed class CompiledFeatureAccess
    {
        private readonly CsEvalEngine _engine;

        internal CompiledFeatureAccess(CsEvalEngine engine)
        {
            _engine = engine;
        }

        internal CsEvalOptions Options => _engine._options;
        internal CsEvalConfig GetOrCreateConfig() => _engine.GetOrCreateConfig();
        internal CsEvalContext GetOrCreateContext() => _engine.GetOrCreateContext(null);
        internal Dictionary<string, object?> CollectEngineVariables() => _engine.CollectEngineVariables();
        internal void ThrowIfDisposed() => _engine.ThrowIfDisposed();
    }

    /// <summary>
    /// Exposes the engine's evaluation context for use by <see cref="CsEvalCompiledExpression{T}"/>.
    /// The context is captured by reference so that variable changes after compilation are visible.
    /// </summary>
    internal CsEvalContext GetContextForCompiled() => GetOrCreateContext(null);
}
