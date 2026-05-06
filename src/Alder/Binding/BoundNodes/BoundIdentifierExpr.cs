namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.Identifier, "Identifier")]
internal sealed partial record BoundIdentifierExpr(string Name, BoundType StaticType, int? LocalId = null) : BoundExpr(StaticType);
