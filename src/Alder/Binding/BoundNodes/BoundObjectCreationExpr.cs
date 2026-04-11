using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundContainer]
internal sealed partial record BoundInitializerEntry(
    string? PropertyName,
    BoundExpr? Value = null,
    BoundExpr? IndexerKey = null,
    ImmutableArray<BoundExpr> Elements = default);

[BoundNode(BoundNodeKind.ObjectCreationExpression, "ObjectCreation")]
internal sealed partial record BoundObjectCreationExpr(
    string TypeName,
    ImmutableArray<BoundExpr> Arguments,
    ImmutableArray<BoundInitializerEntry> InitializerEntries,
    BoundType StaticType) : BoundExpr(StaticType);
