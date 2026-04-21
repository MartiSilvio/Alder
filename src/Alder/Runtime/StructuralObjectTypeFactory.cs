using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Alder.Runtime;

internal sealed record StructuralObjectMember(string Name, Type Type);

internal sealed class StructuralObjectTypeInfo
{
    private readonly ImmutableDictionary<string, int> _memberIndexes;
    private readonly ImmutableDictionary<string, int> _memberIndexesIgnoreCase;

    internal StructuralObjectTypeInfo(ImmutableArray<StructuralObjectMember> members)
    {
        Members = members;

        var indexes = ImmutableDictionary.CreateBuilder<string, int>(StringComparer.Ordinal);
        var indexesIgnoreCase = ImmutableDictionary.CreateBuilder<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < members.Length; i++)
        {
            indexes[members[i].Name] = i;
            indexesIgnoreCase[members[i].Name] = i;
        }

        _memberIndexes = indexes.ToImmutable();
        _memberIndexesIgnoreCase = indexesIgnoreCase.ToImmutable();
    }

    public Type RuntimeType => typeof(StructuralObjectValue);
    public ImmutableArray<StructuralObjectMember> Members { get; }

    internal bool TryGetIndex(string name, bool isCaseSensitive, out int index)
    {
        var map = isCaseSensitive ? _memberIndexes : _memberIndexesIgnoreCase;
        return map.TryGetValue(name, out index);
    }
}

internal sealed class StructuralObjectValue : IReadOnlyDictionary<string, object?>
{
    private readonly object?[] _values;

    internal StructuralObjectValue(StructuralObjectTypeInfo typeInfo, object?[] values)
    {
        if (typeInfo.Members.Length != values.Length)
            throw new ArgumentException("Structural object value count does not match member count.", nameof(values));

        TypeInfo = typeInfo;
        _values = values;
    }

    internal StructuralObjectTypeInfo TypeInfo { get; }
    internal object? GetValue(int index) => _values[index];

    internal bool TryGetValue(string name, bool isCaseSensitive, out object? value)
    {
        if (TypeInfo.TryGetIndex(name, isCaseSensitive, out var index))
        {
            value = _values[index];
            return true;
        }

        value = null;
        return false;
    }

    public IEnumerable<string> Keys => TypeInfo.Members.Select(static member => member.Name);
    public IEnumerable<object?> Values => _values;
    public int Count => _values.Length;

    public object? this[string key]
    {
        get
        {
            if (TryGetValue(key, isCaseSensitive: true, out var value))
                return value;

            throw new KeyNotFoundException(key);
        }
    }

    public bool ContainsKey(string key) => TypeInfo.TryGetIndex(key, isCaseSensitive: true, out _);
    public bool TryGetValue(string key, out object? value) => TryGetValue(key, isCaseSensitive: true, out value);

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        for (var i = 0; i < _values.Length; i++)
            yield return new KeyValuePair<string, object?>(TypeInfo.Members[i].Name, _values[i]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal static class StructuralObjectTypeFactory
{
    private static readonly ConcurrentDictionary<StructuralObjectShapeKey, StructuralObjectTypeInfo> Cache = new();

    public static StructuralObjectTypeInfo GetOrCreate(ImmutableArray<StructuralObjectMember> members)
    {
        if (members.IsDefaultOrEmpty)
            throw new ArgumentException("Structural objects must contain at least one member.", nameof(members));

        return Cache.GetOrAdd(new StructuralObjectShapeKey(members), static key => new StructuralObjectTypeInfo(key.Members));
    }

    public static StructuralObjectValue Create(string[] memberNames, Type[] memberTypes, object?[] values)
    {
        if (memberNames.Length != memberTypes.Length)
            throw new ArgumentException("Structural object member metadata is inconsistent.");

        var members = new StructuralObjectMember[memberNames.Length];
        for (var i = 0; i < memberNames.Length; i++)
            members[i] = new StructuralObjectMember(memberNames[i], memberTypes[i]);

        return Create(GetOrCreate(ImmutableArray.Create(members)), values);
    }

    public static StructuralObjectValue CreateUntyped(string[] memberNames, object?[] values)
    {
        var members = new StructuralObjectMember[memberNames.Length];
        for (var i = 0; i < memberNames.Length; i++)
            members[i] = new StructuralObjectMember(memberNames[i], typeof(object));

        return Create(GetOrCreate(ImmutableArray.Create(members)), values);
    }

    public static StructuralObjectValue Create(StructuralObjectTypeInfo typeInfo, object?[] values) => new(typeInfo, values);

    private readonly struct StructuralObjectShapeKey : IEquatable<StructuralObjectShapeKey>
    {
        public StructuralObjectShapeKey(ImmutableArray<StructuralObjectMember> members) => Members = members;

        public ImmutableArray<StructuralObjectMember> Members { get; }

        public bool Equals(StructuralObjectShapeKey other)
        {
            if (Members.Length != other.Members.Length)
                return false;

            for (var i = 0; i < Members.Length; i++)
            {
                if (Members[i] != other.Members[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object? obj) => obj is StructuralObjectShapeKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;

                foreach (var member in Members)
                {
                    hash = (hash * 31) + member.Name.GetHashCode();
                    hash = (hash * 31) + member.Type.GetHashCode();
                }

                return hash;
            }
        }
    }
}
