using System.Diagnostics.CodeAnalysis;

namespace Alder.Aot;

/// <summary>
/// Represents a closed CLR type whose public constructors are rooted for trim-sensitive execution.
/// </summary>
public readonly struct RootedType : IEquatable<RootedType>
{
    public RootedType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        Type = type;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type Type { get; }

    public bool Equals(RootedType other) => Type == other.Type;

    public override bool Equals(object? obj) => obj is RootedType other && Equals(other);

    public override int GetHashCode() => Type == null ? 0 : Type.GetHashCode();

    public override string ToString() => Type?.ToString() ?? string.Empty;

    public static bool operator ==(RootedType left, RootedType right) => left.Equals(right);

    public static bool operator !=(RootedType left, RootedType right) => !left.Equals(right);
}
