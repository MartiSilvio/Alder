namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.Conversion, "Cast")]
internal sealed partial record BoundCastExpr(
    BoundExpr Expression,
    Type TargetType,
    Type? SourceStaticType,
    BoundType StaticType) : BoundExpr(StaticType);
