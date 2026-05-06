using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Alder.Runtime.Collections;

internal sealed class BoundedConcurrentCache<TKey, TValue>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _entries = new();
    private readonly ConcurrentQueue<TKey> _insertionOrder = new();
    private readonly int _capacity;

    internal BoundedConcurrentCache(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
    }

    internal bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) =>
        _entries.TryGetValue(key, out value);

    internal TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        if (_entries.TryGetValue(key, out var existing))
            return existing;

        var value = valueFactory(key);
        if (_entries.TryAdd(key, value))
        {
            RecordAdd(key);
            return value;
        }

        return _entries.TryGetValue(key, out existing) ? existing : value;
    }

    internal bool TryAdd(TKey key, TValue value)
    {
        if (!_entries.TryAdd(key, value))
            return false;

        RecordAdd(key);
        return true;
    }

    private void RecordAdd(TKey key)
    {
        _insertionOrder.Enqueue(key);
        while (_entries.Count > _capacity && _insertionOrder.TryDequeue(out var oldest))
            _entries.TryRemove(oldest, out _);
    }
}
