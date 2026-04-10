namespace Alder.Attributes;

/// <summary>
/// Marks a method as callable from Alder expressions.
/// On a module with <see cref="AlderModuleAttribute.ExplicitOnly"/> set, only attributed methods are accessible.
/// On a class scanned via <c>RegisterFromAssembly</c>, registers as a global function.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AlderFunctionAttribute : Attribute
{
    /// <summary>
    /// The expression-visible name. Falls back to the method name when <c>null</c>.
    /// </summary>
    public string? Name { get; }

    public AlderFunctionAttribute() { }

    /// <param name="name">The name used to call this function in expressions.</param>
    public AlderFunctionAttribute(string name)
    {
        Name = name;
    }
}