namespace Alder.Attributes;

/// <summary>
/// Marks a type as an Alder module whose members can be reached with <c>ModuleName.Member</c> syntax.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class AlderModuleAttribute : Attribute
{
    /// <summary>
    /// Name exposed to expressions, for example <c>Math</c>.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// When <c>true</c>, only members marked with <see cref="AlderFunctionAttribute"/> are exposed.
    /// </summary>
    public bool ExplicitOnly { get; init; } = false;

    public AlderModuleAttribute() { }

    /// <param name="name">The name used to access this module in expressions.</param>
    public AlderModuleAttribute(string name)
    {
        Name = name;
    }
}
