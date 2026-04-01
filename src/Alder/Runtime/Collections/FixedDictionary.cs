using System.Diagnostics.CodeAnalysis;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace Alder.Runtime.Collections;

internal sealed class FixedDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
#if NET8_0_OR_GREATER
    public static readonly FixedDictionary<TKey, TValue> Empty =
        new(FrozenDictionary<TKey, TValue>.Empty);

    private readonly FrozenDictionary<TKey, TValue> _inner;

    private FixedDictionary(FrozenDictionary<TKey, TValue> inner) => _inner = inner;

    internal static FixedDictionary<TKey, TValue> Create(Dictionary<TKey, TValue> source)
        => new(source.ToFrozenDictionary(source.Comparer));

    internal static FixedDictionary<TKey, TValue> Create(Dictionary<TKey, TValue> source, IEqualityComparer<TKey> comparer)
        => new(source.ToFrozenDictionary(comparer));

    internal static FixedDictionary<TKey, TValue> Create(IEnumerable<KeyValuePair<TKey, TValue>> source)
        => new(source.ToFrozenDictionary());

    internal static FixedDictionary<TKey, TValue> Create(IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> comparer)
        => new(source.ToFrozenDictionary(comparer));

    internal static FixedDictionary<TKey, TValue> Create<TSource>(
        IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector)
        => new(source.ToFrozenDictionary(keySelector, valueSelector));

    internal static FixedDictionary<TKey, TValue> Create<TSource>(
        IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector,
        IEqualityComparer<TKey> comparer)
        => new(source.ToFrozenDictionary(keySelector, valueSelector, comparer));

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _inner.TryGetValue(key, out value);
    public bool ContainsKey(TKey key) => _inner.ContainsKey(key);
    public TValue this[TKey key] => _inner[key];
    public IEnumerable<TKey> Keys => _inner.Keys;
    public IEnumerable<TValue> Values => _inner.Values;
    public int Count => _inner.Count;
    public TValue GetValueOrDefault(TKey key, TValue defaultValue) => _inner.TryGetValue(key, out var v) ? v : defaultValue;
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

#else
    public static readonly FixedDictionary<TKey, TValue> Empty = new(new Dictionary<TKey, TValue>());

    private readonly Dictionary<TKey, TValue> _inner;

    private FixedDictionary(Dictionary<TKey, TValue> source) => _inner = source;

    internal static FixedDictionary<TKey, TValue> Create(Dictionary<TKey, TValue> source)
        => new(new Dictionary<TKey, TValue>(source, source.Comparer));

    internal static FixedDictionary<TKey, TValue> Create(Dictionary<TKey, TValue> source, IEqualityComparer<TKey> comparer)
        => new(new Dictionary<TKey, TValue>(source, comparer));

    internal static FixedDictionary<TKey, TValue> Create(IEnumerable<KeyValuePair<TKey, TValue>> source)
    {
        var dict = source is ICollection<KeyValuePair<TKey, TValue>> c
            ? new Dictionary<TKey, TValue>(c.Count)
            : new Dictionary<TKey, TValue>();
        foreach (var kvp in source) dict[kvp.Key] = kvp.Value;
        return new(dict);
    }

    internal static FixedDictionary<TKey, TValue> Create(IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> comparer)
    {
        var dict = source is ICollection<KeyValuePair<TKey, TValue>> c
            ? new Dictionary<TKey, TValue>(c.Count, comparer)
            : new Dictionary<TKey, TValue>(comparer);
        foreach (var kvp in source) dict[kvp.Key] = kvp.Value;
        return new(dict);
    }

    internal static FixedDictionary<TKey, TValue> Create<TSource>(
        IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector)
    {
        var dict = source is ICollection<TSource> c
            ? new Dictionary<TKey, TValue>(c.Count)
            : new Dictionary<TKey, TValue>();
        foreach (var item in source) dict[keySelector(item)] = valueSelector(item);
        return new(dict);
    }

    internal static FixedDictionary<TKey, TValue> Create<TSource>(
        IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector,
        IEqualityComparer<TKey> comparer)
    {
        var dict = source is ICollection<TSource> c
            ? new Dictionary<TKey, TValue>(c.Count, comparer)
            : new Dictionary<TKey, TValue>(comparer);
        foreach (var item in source) dict[keySelector(item)] = valueSelector(item);
        return new(dict);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _inner.TryGetValue(key, out value);
    public bool ContainsKey(TKey key) => _inner.ContainsKey(key);
    public TValue this[TKey key] => _inner[key];
    public IEnumerable<TKey> Keys => _inner.Keys;
    public IEnumerable<TValue> Values => _inner.Values;
    public int Count => _inner.Count;
    public TValue GetValueOrDefault(TKey key, TValue defaultValue) => _inner.TryGetValue(key, out var v) ? v : defaultValue;
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
#endif
}
