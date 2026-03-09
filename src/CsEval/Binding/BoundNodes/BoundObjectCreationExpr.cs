using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundInitializerEntry(
    string? PropertyName,
    BoundExpr Value);

internal sealed record BoundObjectCreationExpr(
    string TypeName,
    ImmutableArray<BoundExpr> Arguments,
    ImmutableArray<BoundInitializerEntry> InitializerEntries,
    Type StaticType) : BoundExpr(StaticType);
