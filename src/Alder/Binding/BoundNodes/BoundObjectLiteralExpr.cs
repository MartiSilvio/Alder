using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundObjectLiteralProperty(
    string? PropertyName,
    BoundExpr Value,
    bool IsSpread);

internal sealed record BoundObjectLiteralExpr(
    ImmutableArray<BoundObjectLiteralProperty> Properties,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ObjectLiteral;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { foreach (var p in Properties) visit(p.Value); }
}
