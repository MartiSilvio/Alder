using System.Runtime.CompilerServices;
using Alder.Binding;
using Alder.Runtime;

namespace Alder;

internal sealed class ExpressionRuntimeState
{
    private readonly ConditionalWeakTable<AlderContext, CachedBoundExpression> _boundExpressionCacheByContext = new();
    private readonly object _boundCacheGate = new();
    private volatile bool _bindingUnavailable;
    private volatile string? _bindingUnavailableReason;
    private int _boundExecutionCount;
    private int _boundFallbackCount;
    private volatile string? _lastBoundFallbackReason;

    internal volatile CompiledExpressionInfo? CompiledInfo;

    internal bool TryGetCachedBoundExpression(AlderContext context, int version, out BoundExpr? bound)
    {
        lock (_boundCacheGate)
        {
            if (_boundExpressionCacheByContext.TryGetValue(context, out var cached) && cached.Version == version)
            {
                bound = cached.Bound;
                return true;
            }
        }

        bound = null;
        return false;
    }

    internal void CacheBoundExpression(AlderContext context, int version, BoundExpr bound)
    {
        lock (_boundCacheGate)
        {
            if (_boundExpressionCacheByContext.TryGetValue(context, out var existing) && existing.Version == version)
                return;

            _boundExpressionCacheByContext.Remove(context);
            _boundExpressionCacheByContext.Add(context, new CachedBoundExpression(version, bound));
        }
    }

    internal bool TryGetBindingUnavailableReason(out string? reason)
    {
        reason = _bindingUnavailableReason;
        return _bindingUnavailable;
    }

    internal void RecordBindingUnavailable(string? reason)
    {
        _bindingUnavailableReason = reason;
        _bindingUnavailable = true;
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

    private sealed record CachedBoundExpression(int Version, BoundExpr Bound);
}
