using System.Runtime.CompilerServices;
using Alder.Binding;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;
using Binder = Alder.Binding.Binder;

namespace Alder;

/// <summary>
/// Represents parsed source that can be evaluated multiple times without re-parsing.
/// </summary>
public sealed class AlderExpression
{
    internal Expr Ast { get; }

    /// <summary>
    /// Gets the original source text.
    /// </summary>
    public string Source { get; }

    private volatile bool _bindingUnavailable;
    private volatile string? _bindingUnavailableReason;
    private int _boundExecutionCount;
    private int _boundFallbackCount;
    private volatile string? _lastBoundFallbackReason;
    private readonly ConditionalWeakTable<AlderContext, CachedBoundExpression> _boundExpressionCacheByContext = new();
    private readonly object _boundCacheGate = new();

    internal AlderExpression(string expression, Expr ast)
    {
        Source = expression;
        Ast = ast;
    }

    /// <summary>
    /// Gets whether a compiled delegate has been produced for this expression.
    /// </summary>
    public bool IsCompiled => CompiledInfo?.Delegate != null;

    /// <summary>
    /// Gets whether this expression can be compiled.
    /// Returns <c>null</c> if compilation has not yet been attempted.
    /// </summary>
    public bool? IsCompilable => CompiledInfo?.IsCompilable;

    /// <summary>
    /// Gets the last recorded compilation failure reason, if any.
    /// </summary>
    public string? CompilationFailureReason => CompiledInfo?.FailureReason;

    internal volatile CompiledExpressionInfo? CompiledInfo;

    /// <summary>
    /// Returns the distinct unbound identifier names present in the parsed tree.
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
        lock (_boundCacheGate)
        {
            if (_boundExpressionCacheByContext.TryGetValue(context, out var cached) && cached.Version == currentVersion)
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

        lock (_boundCacheGate)
        {
            if (_boundExpressionCacheByContext.TryGetValue(context, out var existing) && existing.Version == currentVersion)
                return existing.Bound;

            var entry = new CachedBoundExpression(currentVersion, bound, bindingContext.LocalCount);
            _boundExpressionCacheByContext.Remove(context);
            _boundExpressionCacheByContext.Add(context, entry);
            return bound;
        }
    }

    internal int GetLocalCount(AlderContext context)
    {
        lock (_boundCacheGate)
        {
            return _boundExpressionCacheByContext.TryGetValue(context, out var cached) ? cached.LocalCount : 0;
        }
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
            _bindingUnavailableReason = ex.Message;
            _bindingUnavailable = true;
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
/// Delegate shape used by the compiled execution pipeline.
/// </summary>
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
/// Captures the compiled delegate and the outcome of the latest compilation attempt.
/// </summary>
internal record CompiledExpressionInfo(
    CompiledExpressionDelegate? Delegate,
    bool IsCompilable,
    string? FailureReason,
    Exception? FailureException = null,
    CompiledPipeline Pipeline = CompiledPipeline.None,
    int? TypeVersion = null);
