namespace CsEval.Aot;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class CsEvalRegisteredAttribute : Attribute
{
    public Type Type { get; }
    public CsEvalRegisteredAttribute(Type type) => Type = type;
}
