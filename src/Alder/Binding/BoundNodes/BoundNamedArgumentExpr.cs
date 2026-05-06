namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.NamedArgument, "NamedArgument")]
internal sealed partial record BoundNamedArgumentExpr(
    string Name,
    BoundExpr Value,
    BoundType StaticType) : BoundExpr(StaticType);
