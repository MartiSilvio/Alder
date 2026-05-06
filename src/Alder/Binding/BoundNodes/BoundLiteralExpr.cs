namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.Literal, "Literal")]
internal sealed partial record BoundLiteralExpr(object? Value, BoundType StaticType) : BoundExpr(StaticType)
{
    internal static BoundLiteralExpr FromValue(object? value)
    {
        var staticType = new BoundType(value?.GetType() ?? typeof(object));
        return new BoundLiteralExpr(value, staticType);
    }
}
