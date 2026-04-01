using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundWithInitializer(string PropertyName, BoundExpr Value);

internal sealed record BoundWithExpr(
    BoundExpr Object,
    ImmutableArray<BoundWithInitializer> Initializers,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.WithExpression;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Object);
        foreach (var init in Initializers) visit(init.Value);
    }
}
