using System.Collections.Immutable;
using System.Reflection;

namespace Alder.Runtime;

internal readonly struct MethodCandidate
{
    public MethodInfo Method { get; }
    public ParameterInfo[] Parameters { get; }
    public ApplicableForm Form { get; }
    public ArgumentParameterMap ArgMap { get; }
    public ImmutableArray<ArgumentConversion> Conversions { get; }
    public MethodInfo? GenericDefinition { get; }

    /// <summary>
    /// Pre-computed lambda return types, one per argument position.
    /// null when no lambda arguments exist. Entries are null for non-lambda arguments
    /// or when inference fails.
    /// </summary>
    public Type?[]? LambdaReturnTypes { get; }

    public MethodCandidate(
        MethodInfo method,
        ParameterInfo[] parameters,
        ApplicableForm form,
        ArgumentParameterMap argMap,
        ImmutableArray<ArgumentConversion> conversions,
        MethodInfo? genericDefinition = null,
        Type?[]? lambdaReturnTypes = null)
    {
        Method = method;
        Parameters = parameters;
        Form = form;
        ArgMap = argMap;
        Conversions = conversions;
        GenericDefinition = genericDefinition;
        LambdaReturnTypes = lambdaReturnTypes;
    }

    public bool IsGeneric => Method.IsGenericMethod;

    public ParameterInfo[] UninstantiatedParameters =>
        GenericDefinition != null
            ? MethodDispatchCache.GetParameters(GenericDefinition)
            : Parameters;

    public ResolvedCall ToResolvedCall() =>
        new(Method, ArgMap, Form, Conversions);
}
