using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.MultiDimIndexAssignment, "MultiDimIndexAssign")]
internal sealed partial record BoundMultiDimIndexAssignExpr(
    BoundExpr Target,
    ImmutableArray<BoundExpr> Indices,
    BoundExpr Value,
    Type? TargetType,
    bool IsArray,
    PropertyInfo? Indexer,
    BoundType StaticType) : BoundExpr(StaticType);
