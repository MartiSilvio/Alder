namespace Alder.Aot;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AlderRegisteredAttribute : Attribute
{
    public Type Type { get; }
    public AlderRegisteredAttribute(Type type) => Type = type;
}
