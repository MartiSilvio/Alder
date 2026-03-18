namespace CsEval.Binding;

internal abstract record BoundExpr(Type StaticType)
{
    internal abstract void EnumerateChildren(Action<BoundExpr> visit);
}
