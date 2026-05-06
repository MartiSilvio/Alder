using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundContainer]
internal sealed partial record BoundObjectLiteralProperty(
    string? PropertyName,
    BoundExpr Value);

[BoundNode(BoundNodeKind.ObjectLiteral, "ObjectLiteral")]
internal sealed partial record BoundObjectLiteralExpr(
    ImmutableArray<BoundObjectLiteralProperty> Properties,
    BoundType StaticType) : BoundExpr(StaticType);
