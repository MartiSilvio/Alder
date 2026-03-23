namespace Alder.Attributes;

/// <summary>
/// Marks a class as an Alder module, making its public methods and properties accessible in expressions
/// via <c>ModuleName.Member</c> syntax.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class AlderModuleAttribute : Attribute
{
    /// <summary>
    /// The name used to access this module in expressions (e.g., "Math" for Math.Abs()).
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// When true, only methods marked with [AlderFunction] are exposed.
    /// When false (default), all public methods are exposed.
    /// </summary>
    public bool ExplicitOnly { get; init; } = false;

    /// <summary>
    /// Creates a module attribute with no explicit name. The module name is specified during registration.
    /// </summary>
    public AlderModuleAttribute() { }

    /// <summary>
    /// Creates a module attribute with the specified name.
    /// </summary>
    /// <param name="name">The name used to access this module in expressions.</param>
    public AlderModuleAttribute(string name)
    {
        Name = name;
    }
}
