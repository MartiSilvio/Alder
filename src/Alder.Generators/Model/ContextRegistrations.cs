using System;
using System.Collections.Immutable;
using System.Linq;

namespace Alder.Generators.Model;

/// <summary>
/// Pipeline value for the incremental generator. Implements structural equality
/// so Roslyn can skip re-generation when inputs haven't changed.
/// </summary>
internal readonly struct ContextRegistrations : IEquatable<ContextRegistrations>
{
    public string Namespace { get; }
    public string ClassName { get; }
    public ImmutableArray<TypeRegistrationModel> Registrations { get; }

    public ContextRegistrations(
        string ns,
        string className,
        ImmutableArray<TypeRegistrationModel> registrations)
    {
        Namespace = ns;
        ClassName = className;
        Registrations = registrations;
    }

    public bool Equals(ContextRegistrations other) =>
        Namespace == other.Namespace
        && ClassName == other.ClassName
        && Registrations.SequenceEqual(other.Registrations);

    public override bool Equals(object obj) => obj is ContextRegistrations other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (Namespace?.GetHashCode() ?? 0);
            hash = hash * 31 + (ClassName?.GetHashCode() ?? 0);
            hash = hash * 31 + Registrations.Length;
            return hash;
        }
    }
}
