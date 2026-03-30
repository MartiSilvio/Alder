using Alder.Binding;

namespace Alder.Interpretation;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class EvaluatesNodeAttribute : Attribute
{
    public BoundNodeKind Kind { get; }

    public EvaluatesNodeAttribute(BoundNodeKind kind)
    {
        Kind = kind;
    }
}
