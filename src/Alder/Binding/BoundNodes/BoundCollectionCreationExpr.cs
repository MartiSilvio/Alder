using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal enum CollectionKind
{
    Array,
    InferredArray,
    TargetTypedCollection
}

internal sealed record BoundCollectionCreationExpr(
    ImmutableArray<BoundExpr> Elements,
    Type ElementType,
    CollectionKind CollectionKind,
    Type? TargetCollectionType,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.CollectionCreation;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { foreach (var e in Elements) visit(e); }
}
