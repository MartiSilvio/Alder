using CsEval.Compilation;
using CsEval.Binding;
using CsEval.Interpretation;
using CsEval.Parsing;
using CsEval.Runtime;
using System.Runtime.CompilerServices;

namespace CsEval;

/// <summary>
/// Represents a pre-parsed expression that can be evaluated multiple times
/// with different variable values without re-parsing.
/// </summary>
public sealed class CsEvalExpression
{
    internal Expr Ast { get; }

    /// <summary>
    /// The original expression string.
    /// </summary>
    public string Source { get; }

    // Compilation state (volatile for thread-safe reads)
    private volatile CompiledExpressionInfo? _compiledInfo;
    private volatile bool _bindingUnavailable;
    private volatile string? _bindingUnavailableReason;
    private int _boundExecutionCount;
    private int _boundFallbackCount;
    private volatile string? _lastBoundFallbackReason;
    private readonly ExpressionCache? _expressionCache;
    private readonly ConditionalWeakTable<CsEvalContext, CachedTypeInferrer> _typeInferrerCacheByContext = new();
    private readonly ConditionalWeakTable<CsEvalContext, CachedBoundExpression> _boundExpressionCacheByContext = new();

    internal CsEvalExpression(string expression, Expr ast) : this(expression, ast, null)
    {
    }

    internal CsEvalExpression(string expression, Expr ast, ExpressionCache? expressionCache)
    {
        Source = expression;
        Ast = ast;
        _expressionCache = expressionCache;
    }

    /// <summary>
    /// Returns true if this expression has been successfully compiled.
    /// </summary>
    public bool IsCompiled => _compiledInfo?.Delegate != null;

    /// <summary>
    /// Returns true if this expression can be compiled.
    /// Returns null if compilation has not been attempted.
    /// </summary>
    public bool? IsCompilable => _compiledInfo?.IsCompilable;

    /// <summary>
    /// Returns the reason why compilation failed, or null if compilation succeeded or hasn't been attempted.
    /// </summary>
    public string? CompilationFailureReason => _compiledInfo?.FailureReason;

    /// <summary>
    /// Attempts to compile this expression. Returns true if successful.
    /// </summary>
    public bool TryCompile()
    {
        return TryCompileCore(static (self, _, _) => self._expressionCache != null
            ? CompiledProviderRegistry.GetOrCompile(self.Source, self.Ast, self._expressionCache)
            : CompiledProviderRegistry.TryCompile(self.Ast));
    }

    internal bool TryCompile(CsEvalOptions options)
    {
        return TryCompileCore(static (self, opts, _) => self._expressionCache != null
            ? CompiledProviderRegistry.GetOrCompile(self.Source, self.Ast, self._expressionCache, opts)
            : CompiledProviderRegistry.TryCompile(self.Ast, opts), options);
    }

    internal bool TryCompile(CsEvalOptions options, CsEvalContext context)
    {
        return TryCompileCore(static (self, opts, ctx) =>
            CompiledProviderRegistry.TryCompile(self.Ast, opts, ctx), options, context);
    }

    private bool TryCompileCore(
        Func<CsEvalExpression, CsEvalOptions?, CsEvalContext?, CompiledExpressionInfo> compile,
        CsEvalOptions? options = null,
        CsEvalContext? context = null)
    {
        if (_compiledInfo != null)
            return _compiledInfo.Delegate != null;

        var info = compile(this, options, context);
        _compiledInfo = info;
        return info.Delegate != null;
    }

    /// <summary>
    /// Compiles this expression. Throws if compilation fails.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when compilation fails.</exception>
    public void Compile()
    {
        if (!TryCompile())
        {
            throw new CsEvalException(
                Diagnostics.DiagnosticDescriptors.StrictCompilationFailed,
                $"Cannot compile expression '{Source}': {_compiledInfo?.FailureReason ?? "Unknown reason"}");
        }
    }

    /// <summary>
    /// Returns the distinct names of unbound identifiers found in the expression AST.
    /// Useful for detecting which variables an expression references.
    /// </summary>
    public IReadOnlyList<string> GetVariables()
    {
        var collector = new VariableCollector();
        collector.Collect(Ast);
        return collector.Variables;
    }

    internal CompiledExpressionInfo? GetCompiledInfo() => _compiledInfo;

    internal BoundExpr GetOrCreateBoundExpression(CsEvalContext context)
    {
        var currentVersion = context.GetTypeInferenceVersion();
        if (_boundExpressionCacheByContext.TryGetValue(context, out var cached) &&
            cached != null &&
            cached.Version == currentVersion)
        {
            return cached.Bound;
        }

        var binder = new CsEval.Binding.Binder();
        var bound = binder.Bind(Ast, new BindingContext(context));
        _boundExpressionCacheByContext.Remove(context);
        _boundExpressionCacheByContext.Add(context, new CachedBoundExpression(currentVersion, bound));
        return bound;
    }

    internal bool TryGetOrCreateBoundExpression(CsEvalContext context, out BoundExpr? bound, out string? failureReason)
    {
        if (_bindingUnavailable)
        {
            bound = null;
            failureReason = _bindingUnavailableReason;
            return false;
        }

        try
        {
            bound = GetOrCreateBoundExpression(context);
            failureReason = null;
            return true;
        }
        catch (BindingNotSupportedException ex)
        {
            _bindingUnavailable = true;
            _bindingUnavailableReason = ex.Message;
            bound = null;
            failureReason = ex.Message;
            return false;
        }
    }

    internal int BoundExecutionCount => _boundExecutionCount;
    internal void RecordBoundExecution() => Interlocked.Increment(ref _boundExecutionCount);
    internal int BoundFallbackCount => _boundFallbackCount;
    internal string? LastBoundFallbackReason => _lastBoundFallbackReason;
    internal void RecordBoundFallback(string? reason)
    {
        Interlocked.Increment(ref _boundFallbackCount);
        if (!string.IsNullOrWhiteSpace(reason))
            _lastBoundFallbackReason = reason;
    }

    internal TypeInferrer GetOrCreateTypeInferrer(CsEvalContext context, int maxDepth)
    {
        var currentVersion = context.GetTypeInferenceVersion();
        if (_typeInferrerCacheByContext.TryGetValue(context, out var cached) &&
            cached.Version == currentVersion)
        {
            return cached.Inferrer;
        }

        var inferrer = new TypeInferrer(context, maxDepth);
        inferrer.InferAll(Ast);
        _typeInferrerCacheByContext.Remove(context);
        _typeInferrerCacheByContext.Add(context, new CachedTypeInferrer(currentVersion, inferrer));
        return inferrer;
    }

    private sealed record CachedBoundExpression(int Version, BoundExpr Bound);
    private sealed record CachedTypeInferrer(int Version, TypeInferrer Inferrer);
}

/// <summary>
/// Delegate type for compiled expressions.
/// </summary>
/// <param name="context">The evaluation context containing variables.</param>
/// <param name="options">The evaluation options.</param>
/// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
/// <returns>The evaluated result.</returns>
internal delegate object? CompiledExpressionDelegate(
    CsEvalContext context,
    CsEvalOptions options,
    CancellationToken cancellationToken);

internal enum CompiledPipeline
{
    None = 0,
    Bound = 1,
    Ast = 2
}

/// <summary>
/// Contains information about a compiled expression.
/// </summary>
/// <param name="Delegate">The compiled delegate, or null if compilation failed.</param>
/// <param name="IsCompilable">Whether the expression can be compiled.</param>
/// <param name="FailureReason">The reason compilation failed, or null if it succeeded.</param>
/// <param name="FailureException">Original failure exception when available.</param>
/// <param name="Pipeline">Which compilation pipeline produced the delegate.</param>
internal record CompiledExpressionInfo(
    CompiledExpressionDelegate? Delegate,
    bool IsCompilable,
    string? FailureReason,
    Exception? FailureException = null,
    CompiledPipeline Pipeline = CompiledPipeline.None);
