using System.Collections.Immutable;

namespace Alder.Binding;

/// <summary>
/// The binder's type representation. Wraps a CLR <see cref="System.Type"/> and optionally
/// carries structural member metadata for types whose CLR type cannot encode their shape
/// (e.g., ExpandoObject-backed anonymous objects where reflection returns no properties).
/// </summary>
internal class BoundType : IEquatable<BoundType>
{
    public Type ClrType { get; }
    public virtual ImmutableDictionary<string, Type>? MemberTypes => null;

    public BoundType(Type clrType)
    {
        ClrType = clrType;
    }

    public bool HasStructuralMembers => MemberTypes is { Count: > 0 };

    public static readonly BoundType Unknown = new BoundUnknownType();
    public static readonly BoundType Void = new BoundVoidType();

    public bool Equals(BoundType? other) =>
        other is not null && GetType() == other.GetType() && ClrType == other.ClrType;
    public override bool Equals(object? obj) => obj is BoundType other && Equals(other);
    public override int GetHashCode() => ClrType.GetHashCode() ^ (GetType().GetHashCode() * 397);
    public override string ToString() => ClrType.Name;

    public static bool operator ==(BoundType? left, BoundType? right) =>
        ReferenceEquals(left, right) || (left is not null && left.Equals(right));
    public static bool operator !=(BoundType? left, BoundType? right) => !(left == right);
}

/// <summary>
/// The binder could not determine the type. Runtime dispatch required.
/// <c>ClrType</c> is <c>typeof(object)</c> for compatibility with runtime dispatch paths.
/// </summary>
internal sealed class BoundUnknownType : BoundType
{
    public BoundUnknownType() : base(typeof(object)) { }
    public override string ToString() => "?";
}

/// <summary>
/// The node is a statement that does not produce a value (if, while, break, etc.).
/// </summary>
internal sealed class BoundVoidType : BoundType
{
    public BoundVoidType() : base(typeof(object)) { }
    public override string ToString() => "void";
}

/// <summary>
/// A resolved type with structural member metadata for types whose CLR type cannot
/// encode their shape (e.g., ExpandoObject-backed anonymous objects).
/// </summary>
internal sealed class BoundStructuralType : BoundType
{
    private readonly ImmutableDictionary<string, Type> _memberTypes;
    public override ImmutableDictionary<string, Type>? MemberTypes => _memberTypes;

    /// <summary>
    /// For named tuple types: ordered element names matching the ValueTuple generic arg positions.
    /// Null entries indicate unnamed elements. Null if not a tuple type.
    /// </summary>
    public ImmutableArray<string?> TupleElementNames { get; }

    public BoundStructuralType(Type clrType, ImmutableDictionary<string, Type> memberTypes,
        ImmutableArray<string?> tupleElementNames = default) : base(clrType)
    {
        _memberTypes = memberTypes;
        TupleElementNames = tupleElementNames;
    }

    /// <summary>
    /// Creates a BoundStructuralType from ordered tuple element names and their CLR types.
    /// </summary>
    internal static BoundStructuralType FromElementNames(Type tupleType, ImmutableArray<string?> names, Type[]? elementTypes = null)
    {
        elementTypes ??= tupleType.GetGenericArguments();
        var members = ImmutableDictionary.CreateBuilder<string, Type>();
        for (var i = 0; i < names.Length && i < elementTypes.Length; i++)
        {
            if (names[i] is { } name)
                members[name] = elementTypes[i];
        }

        return new BoundStructuralType(tupleType, members.ToImmutable(), names);
    }
}
