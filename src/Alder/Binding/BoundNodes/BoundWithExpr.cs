using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

[BoundContainer]
internal sealed partial record BoundWithInitializer(string PropertyName, BoundExpr Value);

[BoundNode(BoundNodeKind.WithExpression, "With")]
internal sealed partial record BoundWithExpr(
    BoundExpr Object,
    ImmutableArray<BoundWithInitializer> Initializers,
    BoundType StaticType) : BoundExpr(StaticType);
