using System;
using System.Collections.Immutable;
using System.Linq;

namespace Alder.Generators.Model;

internal readonly struct BoundNodeModel : IEquatable<BoundNodeModel>
{
    public int KindValue { get; init; }
    public string KindName { get; init; }
    public string VisitorName { get; init; }
    public string TypeFullName { get; init; }
    public string TypeShortName { get; init; }
    public bool ChainFlatten { get; init; }
    public bool HasRevisitHook { get; init; }
    public bool ManualRewrite { get; init; }
    public ImmutableArray<ChildInfo> Children { get; init; }

    public bool Equals(BoundNodeModel other) =>
        KindValue == other.KindValue &&
        KindName == other.KindName &&
        VisitorName == other.VisitorName &&
        TypeFullName == other.TypeFullName &&
        TypeShortName == other.TypeShortName &&
        ChainFlatten == other.ChainFlatten &&
        HasRevisitHook == other.HasRevisitHook &&
        ManualRewrite == other.ManualRewrite &&
        Children.IsDefault == other.Children.IsDefault &&
        (Children.IsDefault || Children.SequenceEqual(other.Children));

    public override bool Equals(object obj) => obj is BoundNodeModel m && Equals(m);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = KindValue;
            hash = hash * 397 ^ (KindName?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (TypeFullName?.GetHashCode() ?? 0);
            return hash;
        }
    }
}

internal readonly struct ChildInfo : IEquatable<ChildInfo>
{
    public string ParameterName { get; init; }
    public ChildKind Kind { get; init; }
    public string ContainerTypeFullName { get; init; }
    public ImmutableArray<ContainerChildInfo> ContainerChildren { get; init; }
    public ImmutableArray<PolymorphicSubtype> Subtypes { get; init; }

    public bool Equals(ChildInfo other) =>
        ParameterName == other.ParameterName &&
        Kind == other.Kind &&
        ContainerTypeFullName == other.ContainerTypeFullName &&
        ContainerChildren.IsDefault == other.ContainerChildren.IsDefault &&
        (ContainerChildren.IsDefault || ContainerChildren.SequenceEqual(other.ContainerChildren)) &&
        Subtypes.IsDefault == other.Subtypes.IsDefault &&
        (Subtypes.IsDefault || Subtypes.SequenceEqual(other.Subtypes));

    public override bool Equals(object obj) => obj is ChildInfo c && Equals(c);
    public override int GetHashCode() => (ParameterName?.GetHashCode() ?? 0) ^ (int)Kind;
}

internal enum ChildKind
{
    Direct,
    Nullable,
    Array,
    NullableArray,
    ContainerArray,
    PolymorphicContainerArray
}

internal readonly struct ContainerChildInfo : IEquatable<ContainerChildInfo>
{
    public string ParameterName { get; init; }
    public ContainerChildKind Kind { get; init; }

    public bool Equals(ContainerChildInfo other) =>
        ParameterName == other.ParameterName && Kind == other.Kind;

    public override bool Equals(object obj) => obj is ContainerChildInfo c && Equals(c);
    public override int GetHashCode() => (ParameterName?.GetHashCode() ?? 0) ^ (int)Kind;
}

internal enum ContainerChildKind
{
    Direct,
    Nullable,
    Array,
    NullableArray
}

internal readonly struct PolymorphicSubtype : IEquatable<PolymorphicSubtype>
{
    public string TypeFullName { get; init; }
    public ImmutableArray<ContainerChildInfo> Children { get; init; }

    public bool Equals(PolymorphicSubtype other) =>
        TypeFullName == other.TypeFullName &&
        Children.IsDefault == other.Children.IsDefault &&
        (Children.IsDefault || Children.SequenceEqual(other.Children));

    public override bool Equals(object obj) => obj is PolymorphicSubtype s && Equals(s);
    public override int GetHashCode() => TypeFullName?.GetHashCode() ?? 0;
}

internal readonly struct BoundContainerModel : IEquatable<BoundContainerModel>
{
    public string TypeFullName { get; init; }
    public string TypeShortName { get; init; }
    public string BaseTypeFullName { get; init; }
    public bool IsAbstract { get; init; }
    public ImmutableArray<ContainerChildInfo> Children { get; init; }

    public bool Equals(BoundContainerModel other) =>
        TypeFullName == other.TypeFullName &&
        TypeShortName == other.TypeShortName &&
        BaseTypeFullName == other.BaseTypeFullName &&
        IsAbstract == other.IsAbstract &&
        Children.IsDefault == other.Children.IsDefault &&
        (Children.IsDefault || Children.SequenceEqual(other.Children));

    public override bool Equals(object obj) => obj is BoundContainerModel m && Equals(m);
    public override int GetHashCode() => TypeFullName?.GetHashCode() ?? 0;
}
