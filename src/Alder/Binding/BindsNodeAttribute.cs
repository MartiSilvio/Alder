namespace Alder.Binding;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class BindsNodeAttribute : Attribute
{
    public Type ExprType { get; }

    public BindsNodeAttribute(Type exprType)
    {
        ExprType = exprType;
    }
}
