using System.Runtime.CompilerServices;
using Alder.Binding;
using Alder.Compilation;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;
using Binder = Alder.Binding.Binder;

namespace Alder;

/// <summary>
/// Represents a pre-parsed expression that can be evaluated multiple times
/// with different variable values without re-parsing.
/// </summary>
public sealed class AlderExpression
{
    internal Expr Ast { get; }

    /// <summary>
    /// The original expression string.
    /// </summary>
    public string Source { get; }

    private volatile bool _bindingUnavailable;
    private volatile string? _bindingUnavailableReason;
    private int _boundExecutionCount;
    private int _boundFallbackCount;
    private volatile string? _lastBoundFallbackReason;
    internal readonly ExpressionCache? _expressionCache;
    private readonly ConditionalWeakTable<AlderContext, CachedBoundExpression> _boundExpressionCacheByContext = new();

    internal AlderExpression(string expression, Expr ast) : this(expression, ast, null)
    {
    }

    internal AlderExpression(string expression, Expr ast, ExpressionCache? expressionCache)
    {
        Source = expression;
        Ast = ast;
        _expressionCache = expressionCache;
    }

    /// <summary>
    /// Returns true if this expression has been successfully compiled.
    /// </summary>
    public bool IsCompiled => CompiledInfo?.Delegate != null;

    /// <summary>
    /// Returns true if this expression can be compiled.
    /// Returns null if compilation has not been attempted.
    /// </summary>
    public bool? IsCompilable => CompiledInfo?.IsCompilable;

    /// <summary>
    /// Returns the reason why compilation failed, or null if compilation succeeded or hasn't been attempted.
    /// </summary>
    public string? CompilationFailureReason => CompiledInfo?.FailureReason;

    internal volatile CompiledExpressionInfo? CompiledInfo;

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

    internal CompiledExpressionInfo? GetCompiledInfo() => CompiledInfo;

    internal BoundExpr GetOrCreateBoundExpression(AlderContext context)
    {
        var currentVersion = context.GetTypeInferenceVersion();
        if (_boundExpressionCacheByContext.TryGetValue(context, out var cached) &&
            cached != null &&
            cached.Version == currentVersion)
        {
            return cached.Bound;
        }

        var sourceText = new Text.SourceText(Source);
        var bindingContext = new BindingContext(context);
        var binder = new Binder(sourceText);

        BoundExpr bound;
        try
        {
            bound = binder.Bind(Ast, bindingContext);
        }
        catch (AlderException)
        {
            var recoveringBinder = new Binder(sourceText);
            bound = recoveringBinder.BindRecovering(Ast, bindingContext);
            var allDiagnostics = recoveringBinder.GetAccumulatedDiagnostics();
            if (allDiagnostics.Count > 0)
            {
                var ex = new AlderException(DiagnosticDescriptors.BindingFailed, allDiagnostics[0].Message);
                ex.SetDiagnostics(allDiagnostics);
                throw ex;
            }
            throw;
        }

        if (bound.HasErrors)
        {
            var allDiagnostics = CollectTreeDiagnostics(bound);
            var ex = new AlderException(
                DiagnosticDescriptors.BindingFailed,
                allDiagnostics.Count > 0 ? allDiagnostics[0].Message : "Expression has binding errors");
            if (allDiagnostics.Count > 0)
                ex.SetDiagnostics(allDiagnostics);
            throw ex;
        }

        var entry = new CachedBoundExpression(currentVersion, bound, bindingContext.LocalCount);
        _boundExpressionCacheByContext.Remove(context);
        _boundExpressionCacheByContext.Add(context, entry);
        return bound;
    }

    internal int GetLocalCount(AlderContext context)
    {
        return _boundExpressionCacheByContext.TryGetValue(context, out var cached) ? cached.LocalCount : 0;
    }

    internal bool TryGetOrCreateBoundExpression(AlderContext context, out BoundExpr? bound, out string? failureReason)
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

    private static IReadOnlyList<AlderDiagnostic> CollectTreeDiagnostics(BoundExpr root)
    {
        var collector = new DiagnosticCollector();
        collector.Walk(root);
        return collector.Diagnostics;
    }

    private sealed record CachedBoundExpression(int Version, BoundExpr Bound, int LocalCount);
}

/// <summary>
/// Delegate type for compiled expressions.
/// </summary>
/// <param name="context">The evaluation context containing variables.</param>
/// <param name="config">The runtime configuration.</param>
/// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
/// <returns>The evaluated result.</returns>
internal delegate object? CompiledExpressionDelegate(
    AlderContext context,
    AlderConfig config,
    ExecutionConstraintState constraintState,
    CancellationToken cancellationToken);

internal enum CompiledPipeline
{
    None = 0,
    Bound = 1
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
