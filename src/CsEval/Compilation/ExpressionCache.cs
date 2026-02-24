using System.Collections.Concurrent;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

/// <summary>
/// Thread-safe cache for compiled expression delegates with bounded capacity and FIFO eviction.
/// Instance-based caching per engine - cache is shared with child engines and cleaned up when root engine is disposed.
/// </summary>
internal sealed class ExpressionCache
{
    private readonly ConcurrentDictionary<string, CompiledExpressionInfo> _cache = new();
    private readonly ConcurrentQueue<string> _insertionOrder = new();
    private readonly int _capacity;

    public ExpressionCache(int capacity = 10_000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
    }

    /// <summary>
    /// Current number of cached entries.
    /// </summary>
    public int Count => _cache.Count;

    public CompiledExpressionInfo GetOrAdd(string key, Func<string, CompiledExpressionInfo> valueFactory)
    {
        if (_cache.TryGetValue(key, out var existing))
            return existing;

        var value = _cache.GetOrAdd(key, valueFactory);

        // Track insertion order for FIFO eviction.
        // Only enqueue if this call actually added the entry (avoid duplicate keys in queue).
        // The ConcurrentDictionary.GetOrAdd may return an existing value if another thread
        // added it first -- in that case we still enqueue (slightly over-tracking is acceptable
        // since eviction is approximate).
        _insertionOrder.Enqueue(key);

        // Evict oldest entries if over capacity.
        // Approximate bounds are acceptable -- briefly exceeding capacity under concurrent
        // access is fine for a performance cache.
        while (_cache.Count > _capacity && _insertionOrder.TryDequeue(out var oldest))
        {
            _cache.TryRemove(oldest, out _);
        }

        return value;
    }

    public bool TryGetValue(string key, out CompiledExpressionInfo value)
    {
        return _cache.TryGetValue(key, out value!);
    }

    public void Clear()
    {
        _cache.Clear();

        // Drain the queue to avoid stale keys
        while (_insertionOrder.TryDequeue(out _)) { }
    }
}

/// <summary>
/// Orchestrates AST-to-IL compilation via <see cref="CompilerContext"/>.
/// </summary>
internal static class ILExpressionCompiler
{
    /// <summary>
    /// Get or create compiled delegate for an expression string.
    /// </summary>
    public static CompiledExpressionInfo GetOrCompile(string expressionText, Expr ast, ExpressionCache cache, CsEvalOptions? options = null)
    {
        return cache.GetOrAdd(expressionText, _ => TryCompile(ast, options));
    }

    /// <summary>
    /// Attempt to compile an AST to a native IL delegate.
    /// </summary>
    public static CompiledExpressionInfo TryCompile(Expr ast, CsEvalOptions? options = null)
    {
        try
        {
            var context = new CsEvalContext(CsEvalConfig.Empty);
            var opts = options ?? CsEvalOptions.Default;
            var (ilDelegate, failureReason) = CompilerContext.TryCompile(ast, context, opts);

            if (ilDelegate != null)
            {
                return new CompiledExpressionInfo(Compiled, true, null);

                object? Compiled(CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct,
                    Func<MethodInfo, object?[], object?[]>? argumentTransformer)
                    => ilDelegate(ctx, opts, ct, argumentTransformer);
            }

            return new CompiledExpressionInfo(null, false, failureReason);
        }
        catch (CsEvalDepthException)
        {
            throw; // Depth limits are recoverable — let them propagate so callers can surface them
        }
        catch (Exception ex)
        {
            return new CompiledExpressionInfo(null, false, ex.Message);
        }
    }
}
