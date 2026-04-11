using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal enum CollectionKind
{
    Array,
    InferredArray,
    TargetTypedCollection
}

[BoundNode(BoundNodeKind.CollectionCreation, "CollectionCreation")]
internal sealed partial record BoundCollectionCreationExpr(
    ImmutableArray<BoundExpr> Elements,
    Type ElementType,
    CollectionKind CollectionKind,
    Type? TargetCollectionType,
    BoundType StaticType) : BoundExpr(StaticType);
