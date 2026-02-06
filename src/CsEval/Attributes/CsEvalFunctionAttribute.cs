namespace CsEval.Attributes;

/// <summary>
/// Marks a method as exposed to CsEval expressions.
/// When used on a module with ExplicitOnly=true, only attributed methods are accessible.
/// When used on a class scanned via RegisterFromAssembly, registers as a global function.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class CsEvalFunctionAttribute : Attribute
{
    /// <summary>
    /// The name used to call this function in expressions.
    /// If null, the method name is used.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Marks the method as exposed, using the method name.
    /// </summary>
    public CsEvalFunctionAttribute() { }

    /// <summary>
    /// Marks the method as exposed with a custom name.
    /// </summary>
    public CsEvalFunctionAttribute(string name)
    {
        Name = name;
    }
}