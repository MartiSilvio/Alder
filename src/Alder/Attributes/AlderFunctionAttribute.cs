namespace Alder.Attributes;

/// <summary>
/// Marks a method as callable from Alder expressions.
/// On an explicit-only module, only attributed members are exposed.
/// On a scanned type outside a module, the method is registered as a global function.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AlderFunctionAttribute : Attribute
{
    /// <summary>
    /// Name exposed to expressions. When null, Alder uses the CLR method name.
    /// </summary>
    public string? Name { get; }

    public AlderFunctionAttribute() { }

    /// <param name="name">The name used to call this function in expressions.</param>
    public AlderFunctionAttribute(string name)
    {
        Name = name;
    }
}
