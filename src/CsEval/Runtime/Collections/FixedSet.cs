using System.Collections;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace CsEval.Runtime.Collections;

internal sealed class FixedSet<T> : IReadOnlyCollection<T>
{
#if NET8_0_OR_GREATER
    public static readonly FixedSet<T> Empty = new(FrozenSet<T>.Empty);

    private readonly FrozenSet<T> _inner;

    private FixedSet(FrozenSet<T> inner) => _inner = inner;

    internal static FixedSet<T> Create(IEnumerable<T> source)
        => new(source.ToFrozenSet());

    internal static FixedSet<T> Create(IEnumerable<T> source, IEqualityComparer<T> comparer)
        => new(source.ToFrozenSet(comparer));

    public bool Contains(T item) => _inner.Contains(item);
    public int Count => _inner.Count;
    public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

#else
    public static readonly FixedSet<T> Empty = new(new HashSet<T>());

    private readonly HashSet<T> _inner;

    private FixedSet(HashSet<T> source) => _inner = source;

    internal static FixedSet<T> Create(IEnumerable<T> source)
        => new(new HashSet<T>(source));

    internal static FixedSet<T> Create(IEnumerable<T> source, IEqualityComparer<T> comparer)
        => new(new HashSet<T>(source, comparer));

    public bool Contains(T item) => _inner.Contains(item);
    public int Count => _inner.Count;
    public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
#endif
}
