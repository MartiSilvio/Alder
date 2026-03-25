namespace Alder.Binding.BoundNodes;

internal sealed record BoundLiteralExpr(object? Value, BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.Literal;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }

    internal static BoundLiteralExpr FromValue(object? value)
    {
        var staticType = new BoundType(value?.GetType() ?? typeof(object));
        return new BoundLiteralExpr(value, staticType);
    }
}
