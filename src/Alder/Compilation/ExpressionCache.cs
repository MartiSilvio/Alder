using System.Collections.Concurrent;
namespace Alder.Compilation;

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
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    /// <summary>
    /// Current number of cached entries.
    /// </summary>
    public int Count => _cache.Count;

    public CompiledExpressionInfo GetOrAdd(string key, Func<string, CompiledExpressionInfo> valueFactory)
    {
        var value = _cache.GetOrAdd(key, valueFactory);

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
