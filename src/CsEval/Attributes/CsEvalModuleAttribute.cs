namespace CsEval.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class CsEvalModuleAttribute : Attribute
{
    /// <summary>
    /// The name used to access this module in expressions (e.g., "Math" for Math.Abs()).
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// When true, only methods marked with [CsEvalFunction] are exposed.
    /// When false (default), all public methods are exposed.
    /// </summary>
    public bool ExplicitOnly { get; init; } = false;

    public CsEvalModuleAttribute() { }

    public CsEvalModuleAttribute(string name)
    {
        Name = name;
    }
}