namespace CsEval.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CsEvalFunctionAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}