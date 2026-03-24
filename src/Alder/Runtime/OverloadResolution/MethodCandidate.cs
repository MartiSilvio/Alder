using System.Reflection;

namespace Alder.Runtime;

internal readonly struct MethodCandidate
{
    public MethodBase Method { get; }
    public ParameterInfo[] Parameters { get; }
    public ApplicableForm Form { get; }
    public ArgumentParameterMap ArgMap { get; }

    public MethodCandidate(MethodBase method, ParameterInfo[] parameters, ApplicableForm form, ArgumentParameterMap argMap)
    {
        Method = method;
        Parameters = parameters;
        Form = form;
        ArgMap = argMap;
    }
}
