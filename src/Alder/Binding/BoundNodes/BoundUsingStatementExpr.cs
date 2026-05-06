namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.UsingStatement, "UsingStatement")]
internal sealed partial record BoundUsingStatementExpr(
    BoundExpr Resource,
    BoundExpr Body,
    BoundType StaticType) : BoundExpr(StaticType);
