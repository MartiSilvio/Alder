using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.ResolvedMultiDimIndexAccess, "ResolvedMultiDimIndexAccess")]
internal sealed partial record BoundResolvedMultiDimIndexAccessExpr(
    BoundExpr Target,
    ImmutableArray<BoundExpr> Indices,
    Type TargetType,
    bool IsArray,
    PropertyInfo? Indexer,
    bool NullSafe,
    BoundType StaticType) : BoundExpr(StaticType);
