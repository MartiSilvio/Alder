using System.Collections.Immutable;
using System.Reflection;
using CsEval.Binding.Plans;
using CsEval.Diagnostics;
using CsEval.Runtime;

namespace CsEval.Binding.Services;

internal sealed class CallBinderService
{
    private readonly CsEvalContext _context;

    public CallBinderService(CsEvalContext context)
    {
        _context = context;
    }

    public BoundCallPlan BindStaticCall(Type declaringType, string methodName, IReadOnlyList<object?> args, bool isCaseSensitive)
    {
        var flags = BindingFlags.Public | BindingFlags.Static;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeCache.GetMethods(declaringType, methodName, flags);
        var sourceTypes = args.Select(static arg => arg?.GetType() ?? typeof(object)).ToArray();
        return BindFromValues(methods, args, sourceTypes, methodName, isStaticCall: true);
    }

    public BoundCallPlan BindInstanceCall(Type targetType, string methodName, IReadOnlyList<object?> args, bool isCaseSensitive)
    {
        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeCache.GetMethods(targetType, methodName, flags);
        var sourceTypes = args.Select(static arg => arg?.GetType() ?? typeof(object)).ToArray();
        return BindFromValues(methods, args, sourceTypes, methodName, isStaticCall: false);
    }

    public BoundCallPlan BindStaticCall(
        Type declaringType,
        string methodName,
        IReadOnlyList<Type> argumentTypes,
        bool isCaseSensitive)
    {
        var flags = BindingFlags.Public | BindingFlags.Static;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeCache.GetMethods(declaringType, methodName, flags);
        return BindFromTypes(methods, argumentTypes, methodName, isStaticCall: true);
    }

    public BoundCallPlan BindInstanceCall(
        Type targetType,
        string methodName,
        IReadOnlyList<Type> argumentTypes,
        bool isCaseSensitive)
    {
        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeCache.GetMethods(targetType, methodName, flags);
        return BindFromTypes(methods, argumentTypes, methodName, isStaticCall: false);
    }

    private static BoundCallPlan BindFromValues(
        MethodInfo[] methods,
        IReadOnlyList<object?> args,
        IReadOnlyList<Type> sourceTypes,
        string methodName,
        bool isStaticCall)
    {
        if (methods.Length == 0)
            throw new CsEvalException(DiagnosticDescriptors.MemberNotFound, "type", methodName);

        var argArray = args as object?[] ?? args.ToArray();
        var bestMethod = CsEval.Runtime.MethodInvoker.FindBestMethod(methods, argArray, CancellationToken.None, out var ambiguous);
        if (ambiguous)
            throw new CsEvalException($"Ambiguous method invocation: '{methodName}'");

        if (bestMethod == null)
            throw new CsEvalException($"No applicable overload found for method '{methodName}'");

        return CreatePlan(bestMethod, sourceTypes, isStaticCall);
    }

    private static BoundCallPlan BindFromTypes(
        MethodInfo[] methods,
        IReadOnlyList<Type> sourceTypes,
        string methodName,
        bool isStaticCall)
    {
        if (methods.Length == 0)
            throw new CsEvalException(DiagnosticDescriptors.MemberNotFound, "type", methodName);

        var argumentTypeArray = sourceTypes as Type[] ?? sourceTypes.ToArray();
        var bestMethod = MethodResolver.TryResolveMethod(methods, argumentTypeArray);
        if (bestMethod == null)
            throw new CsEvalException($"No applicable overload found for method '{methodName}'");

        return CreatePlan(bestMethod, sourceTypes, isStaticCall);
    }

    private static BoundCallPlan CreatePlan(
        MethodInfo selectedMethod,
        IReadOnlyList<Type> sourceTypes,
        bool isStaticCall)
    {
        var parameters = MethodDispatchCache.GetParameters(selectedMethod);
        var conversions = ImmutableArray.CreateBuilder<BoundConversionPlan>(sourceTypes.Count);

        for (var i = 0; i < sourceTypes.Count; i++)
        {
            var sourceType = sourceTypes[i];
            var targetType = ResolveTargetTypeForArgument(parameters, i);
            var isIdentity = sourceType == targetType;
            conversions.Add(new BoundConversionPlan(sourceType, targetType, isIdentity));
        }

        return new BoundCallPlan(selectedMethod, conversions.ToImmutable(), isStaticCall);
    }

    private static Type ResolveTargetTypeForArgument(ParameterInfo[] parameters, int argumentIndex)
    {
        if (parameters.Length == 0)
            return typeof(object);

        var lastParamIndex = parameters.Length - 1;
        var lastParameter = parameters[lastParamIndex];
        var isParams = lastParameter.IsDefined(typeof(ParamArrayAttribute), false);

        if (isParams && argumentIndex >= lastParamIndex)
        {
            return lastParameter.ParameterType.GetElementType() ?? typeof(object);
        }

        if (argumentIndex >= parameters.Length)
            return typeof(object);

        var parameterType = parameters[argumentIndex].ParameterType;
        return parameterType.IsByRef
            ? parameterType.GetElementType() ?? typeof(object)
            : parameterType;
    }
}
