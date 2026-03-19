using CsEval.Text;

namespace CsEval.Binding;

internal abstract record BoundExpr(Type StaticType)
{
    internal TextSpan Span { get; init; }
    internal abstract void EnumerateChildren(Action<BoundExpr> visit);
}
