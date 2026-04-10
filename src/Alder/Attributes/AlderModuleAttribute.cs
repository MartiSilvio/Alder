namespace Alder.Attributes;

/// <summary>
/// Marks a class as an Alder module, making its public members accessible via <c>ModuleName.Member</c> syntax.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class AlderModuleAttribute : Attribute
{
    /// <summary>
    /// The expression-visible module name (e.g., <c>"Math"</c> for <c>Math.Abs()</c>).
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// When <c>true</c>, only methods marked with <see cref="AlderFunctionAttribute"/> are exposed.
    /// </summary>
    public bool ExplicitOnly { get; init; } = false;

    public AlderModuleAttribute() { }

    /// <param name="name">The name used to access this module in expressions.</param>
    public AlderModuleAttribute(string name)
    {
        Name = name;
    }
}
