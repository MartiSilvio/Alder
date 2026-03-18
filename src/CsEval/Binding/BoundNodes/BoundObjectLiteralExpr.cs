using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundObjectLiteralProperty(
    string? PropertyName,
    BoundExpr Value,
    bool IsSpread);

internal sealed record BoundObjectLiteralExpr(
    ImmutableArray<BoundObjectLiteralProperty> Properties,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { foreach (var p in Properties) visit(p.Value); }
}
