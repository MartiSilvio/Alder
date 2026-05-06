using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.ForEachStatement, "ForEach")]
internal sealed partial record BoundForEachExpr(
    string VariableName,
    BoundExpr Collection,
    ImmutableArray<BoundExpr> Body,
    Type ElementType,
    BoundType StaticType,
    int? LocalId = null) : BoundExpr(StaticType);
