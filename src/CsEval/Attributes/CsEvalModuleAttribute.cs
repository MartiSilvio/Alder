namespace CsEval.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class CsEvalModuleAttribute(string name) : Attribute
    {
        public string Name { get; } = name;
    }
}
