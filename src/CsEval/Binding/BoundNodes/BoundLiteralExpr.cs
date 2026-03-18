namespace CsEval.Binding.BoundNodes;

internal sealed record BoundLiteralExpr(object? Value, Type StaticType) : BoundExpr(StaticType)
{
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }

    internal static BoundLiteralExpr FromValue(object? value)
    {
        var staticType = value?.GetType() ?? typeof(object);
        return new BoundLiteralExpr(value, staticType);
    }
}
