using System.Collections.Immutable;

namespace Alder.Binding;

/// <summary>
/// The binder's type representation. Wraps a CLR <see cref="System.Type"/> and optionally
/// carries structural member metadata for types whose CLR type cannot encode their shape
/// (e.g., ExpandoObject-backed anonymous objects where reflection returns no properties).
/// </summary>
internal readonly struct BoundType : IEquatable<BoundType>
{
    public Type ClrType { get; }
    public ImmutableDictionary<string, Type>? MemberTypes { get; }

    public BoundType(Type clrType)
    {
        ClrType = clrType;
        MemberTypes = null;
    }

    public BoundType(Type clrType, ImmutableDictionary<string, Type> memberTypes)
    {
        ClrType = clrType;
        MemberTypes = memberTypes;
    }

    public bool HasStructuralMembers => MemberTypes is { Count: > 0 };

    public bool Equals(BoundType other) => ClrType == other.ClrType;
    public override bool Equals(object? obj) => obj is BoundType other && Equals(other);
    public override int GetHashCode() => ClrType.GetHashCode();
    public override string ToString() => ClrType.Name;

    public static bool operator ==(BoundType left, BoundType right) => left.ClrType == right.ClrType;
    public static bool operator !=(BoundType left, BoundType right) => left.ClrType != right.ClrType;
}
