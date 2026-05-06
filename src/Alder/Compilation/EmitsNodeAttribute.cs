using Alder.Binding;

namespace Alder.Compilation;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
internal sealed class EmitsNodeAttribute : Attribute
{
    public BoundNodeKind Kind { get; }
    public Type? BoundExprType { get; }

    public EmitsNodeAttribute(BoundNodeKind kind)
    {
        Kind = kind;
    }

    public EmitsNodeAttribute(BoundNodeKind kind, Type boundExprType)
    {
        Kind = kind;
        BoundExprType = boundExprType;
    }
}
