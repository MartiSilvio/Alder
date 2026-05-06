using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.IsPatternExpression, "IsPattern")]
internal sealed partial record BoundIsPatternExpr(
    BoundExpr Expression,
    Pattern Pattern,
    BoundType StaticType) : BoundExpr(StaticType);
