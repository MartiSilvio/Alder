using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundInitializerEntry(
    string? PropertyName,
    BoundExpr Value,
    BoundExpr? IndexerKey = null);

internal sealed record BoundObjectCreationExpr(
    string TypeName,
    ImmutableArray<BoundExpr> Arguments,
    ImmutableArray<BoundInitializerEntry> InitializerEntries,
    Type StaticType) : BoundExpr(StaticType);
