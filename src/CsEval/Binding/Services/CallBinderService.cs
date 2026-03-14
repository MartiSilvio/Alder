using System.Collections.Immutable;
using CsEval.Binding.Plans;
using CsEval.Diagnostics;
using CsEval.Runtime;

namespace CsEval.Binding.Services;

internal sealed class CallBinderService
{
    private const int ApplicableBaseScore = 1000;
    private const int ExpandedBaseScore = 500;
    private const int ExactMatchScore = 100;
    private const int AssignableMatchScore = 10;
    private const int ImplicitConversionScore = 1;
    private const int DefaultArgumentPenalty = 10;

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

        var methods = _context.TypeMetadata.GetMethods(declaringType, methodName, flags);
        var sourceTypes = args.Select(static arg => arg?.GetType() ?? typeof(object)).ToArray();
        return BindFromTypes(methods, sourceTypes, methodName, isStaticCall: true);
    }

    public BoundCallPlan BindInstanceCall(Type targetType, string methodName, IReadOnlyList<object?> args, bool isCaseSensitive)
    {
        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeMetadata.GetMethods(targetType, methodName, flags);
        var sourceTypes = args.Select(static arg => arg?.GetType() ?? typeof(object)).ToArray();
        return BindFromTypes(methods, sourceTypes, methodName, isStaticCall: false);
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

        var methods = _context.TypeMetadata.GetMethods(declaringType, methodName, flags);
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

        var methods = _context.TypeMetadata.GetMethods(targetType, methodName, flags);
        return BindFromTypes(methods, argumentTypes, methodName, isStaticCall: false);
    }

    private static BoundCallPlan BindFromTypes(
        MethodInfo[] methods,
        IReadOnlyList<Type> sourceTypes,
        string methodName,
        bool isStaticCall)
    {
        if (methods.Length == 0)
            throw new CsEvalException(DiagnosticDescriptors.MemberNotFound, "type", methodName);

        // Unknown static argument types (object) are common when an upstream call stays
        // runtime-bound (e.g., extension-method/lambda-heavy chains). If multiple overloads
        // exist, bind-time resolution can select the wrong method (typically object overloads),
        // so defer to runtime dispatch instead of committing a potentially incorrect plan.
        if (methods.Length > 1 && sourceTypes.Any(static sourceType => sourceType == typeof(object)))
            throw new CsEvalException($"Call '{methodName}' requires runtime overload resolution");

        BoundCallPlan? bestPlan = null;
        var bestScore = int.MinValue;
        var ambiguous = false;

        foreach (var method in methods)
        {
            if (!TryCreatePlan(method, sourceTypes, isStaticCall, out var candidatePlan, out var score))
                continue;

            if (score > bestScore)
            {
                bestPlan = candidatePlan;
                bestScore = score;
                ambiguous = false;
                continue;
            }

            if (score != bestScore || bestPlan == null)
                continue;

            var tieBreak = CompareMethodSpecificity(bestPlan.SelectedMethod, method);
            if (tieBreak < 0)
            {
                bestPlan = candidatePlan;
                ambiguous = false;
            }
            else if (tieBreak == 0)
            {
                ambiguous = true;
            }
        }

        if (ambiguous)
            throw new CsEvalException($"Ambiguous method invocation: '{methodName}'");

        if (bestPlan == null)
            throw new CsEvalException($"No applicable overload found for method '{methodName}'");

        return bestPlan;
    }

    private static bool TryCreatePlan(
        MethodInfo selectedMethod,
        IReadOnlyList<Type> sourceTypes,
        bool isStaticCall,
        out BoundCallPlan plan,
        out int score)
    {
        plan = null!;
        score = int.MinValue;

        if (selectedMethod.ContainsGenericParameters)
            return false;

        var parameters = MethodDispatchCache.GetParameters(selectedMethod);
        if (parameters.Any(static parameter => parameter.ParameterType.IsByRef))
            return false;

        var normalDirectMapping = false;
        if (!TryBuildNormalPlan(
                parameters,
                sourceTypes,
                out var normalConversions,
                out var normalBindings,
                out var normalScore,
                out normalDirectMapping))
        {
            normalScore = int.MinValue;
        }

        var expandedScore = int.MinValue;
        var expandedDirectMapping = false;
        ImmutableArray<BoundConversionPlan> expandedConversions = default;
        ImmutableArray<BoundParameterBinding> expandedBindings = default;

        if (TryBuildExpandedParamsPlan(
                parameters,
                sourceTypes,
                out var conversions,
                out var bindings,
                out var scoreCandidate,
                out expandedDirectMapping))
        {
            expandedScore = scoreCandidate;
            expandedConversions = conversions;
            expandedBindings = bindings;
        }

        if (normalScore == int.MinValue && expandedScore == int.MinValue)
            return false;

        var useExpanded = expandedScore > normalScore;
        var selectedConversions = useExpanded ? expandedConversions : normalConversions;
        var selectedBindings = useExpanded ? expandedBindings : normalBindings;
        score = useExpanded ? expandedScore : normalScore;
        var isDirectArgumentMapping = useExpanded
            ? expandedDirectMapping
            : normalDirectMapping;

        plan = new BoundCallPlan(
            selectedMethod,
            selectedConversions,
            selectedBindings,
            isStaticCall,
            IsDirectArgumentMapping: isDirectArgumentMapping);
        return true;
    }

    private static bool TryBuildNormalPlan(
        ParameterInfo[] parameters,
        IReadOnlyList<Type> sourceTypes,
        out ImmutableArray<BoundConversionPlan> conversions,
        out ImmutableArray<BoundParameterBinding> bindings,
        out int score,
        out bool isDirectArgumentMapping)
    {
        var sourceCount = sourceTypes.Count;
        var parameterCount = parameters.Length;
        var lastParameterIndex = parameterCount - 1;
        var hasParams = parameterCount > 0 && parameters[lastParameterIndex].IsDefined(typeof(ParamArrayAttribute), false);
        isDirectArgumentMapping = sourceCount == parameterCount;

        if (sourceCount > parameterCount)
        {
            conversions = default;
            bindings = default;
            score = int.MinValue;
            isDirectArgumentMapping = false;
            return false;
        }

        var conversionBuilder = ImmutableArray.CreateBuilder<BoundConversionPlan>(sourceCount);
        var bindingBuilder = ImmutableArray.CreateBuilder<BoundParameterBinding>(parameterCount);
        var runningScore = ApplicableBaseScore;
        var defaultsUsed = 0;

        for (var i = 0; i < sourceCount; i++)
        {
            var targetType = GetNormalFormTargetType(parameters, i, hasParams, sourceCount == parameterCount);
            if (targetType == null)
            {
                conversions = default;
                bindings = default;
                score = int.MinValue;
                isDirectArgumentMapping = false;
                return false;
            }

            var argScore = ScoreArgumentType(sourceTypes[i], targetType);
            if (argScore < 0)
            {
                conversions = default;
                bindings = default;
                score = int.MinValue;
                isDirectArgumentMapping = false;
                return false;
            }

            if (sourceTypes[i] != targetType)
                isDirectArgumentMapping = false;

            runningScore += argScore;
            conversionBuilder.Add(new BoundConversionPlan(
                sourceTypes[i],
                targetType,
                sourceTypes[i] == targetType));

            bindingBuilder.Add(new BoundParameterBinding(
                BoundParameterBindingKind.Argument,
                i,
                i,
                SourceArgumentCount: 1));
        }

        for (var i = sourceCount; i < parameterCount; i++)
        {
            if (hasParams && i == lastParameterIndex)
            {
                isDirectArgumentMapping = false;
                bindingBuilder.Add(new BoundParameterBinding(
                    BoundParameterBindingKind.ParamsArray,
                    i,
                    SourceArgumentIndex: sourceCount,
                    SourceArgumentCount: 0));
                continue;
            }

            if (!parameters[i].HasDefaultValue)
            {
                conversions = default;
                bindings = default;
                score = int.MinValue;
                isDirectArgumentMapping = false;
                return false;
            }

            defaultsUsed++;
            isDirectArgumentMapping = false;
            bindingBuilder.Add(new BoundParameterBinding(
                BoundParameterBindingKind.DefaultValue,
                i,
                SourceArgumentIndex: -1,
                SourceArgumentCount: 0));
        }

        conversions = conversionBuilder.ToImmutable();
        bindings = bindingBuilder.ToImmutable();
        score = runningScore - (defaultsUsed * DefaultArgumentPenalty);
        return true;
    }

    private static bool TryBuildExpandedParamsPlan(
        ParameterInfo[] parameters,
        IReadOnlyList<Type> sourceTypes,
        out ImmutableArray<BoundConversionPlan> conversions,
        out ImmutableArray<BoundParameterBinding> bindings,
        out int score,
        out bool isDirectArgumentMapping)
    {
        conversions = default;
        bindings = default;
        score = int.MinValue;
        isDirectArgumentMapping = false;

        if (parameters.Length == 0)
            return false;

        var lastParameterIndex = parameters.Length - 1;
        var paramsParameter = parameters[lastParameterIndex];
        if (!paramsParameter.IsDefined(typeof(ParamArrayAttribute), false))
            return false;

        var elementType = paramsParameter.ParameterType.GetElementType();
        if (elementType == null)
            return false;

        if (sourceTypes.Count < lastParameterIndex)
            return false;

        var conversionBuilder = ImmutableArray.CreateBuilder<BoundConversionPlan>(sourceTypes.Count);
        var bindingBuilder = ImmutableArray.CreateBuilder<BoundParameterBinding>(parameters.Length);
        var runningScore = ExpandedBaseScore;
        var defaultsUsed = 0;

        for (var i = 0; i < lastParameterIndex; i++)
        {
            if (i < sourceTypes.Count)
            {
                var targetType = GetParameterTargetType(parameters[i]);
                var argScore = ScoreArgumentType(sourceTypes[i], targetType);
                if (argScore < 0)
                    return false;

                runningScore += argScore;
                conversionBuilder.Add(new BoundConversionPlan(
                    sourceTypes[i],
                    targetType,
                    sourceTypes[i] == targetType));
                bindingBuilder.Add(new BoundParameterBinding(
                    BoundParameterBindingKind.Argument,
                    i,
                    i,
                    SourceArgumentCount: 1));
                continue;
            }

            if (!parameters[i].HasDefaultValue)
                return false;

            defaultsUsed++;
            bindingBuilder.Add(new BoundParameterBinding(
                BoundParameterBindingKind.DefaultValue,
                i,
                SourceArgumentIndex: -1,
                SourceArgumentCount: 0));
        }

        for (var i = lastParameterIndex; i < sourceTypes.Count; i++)
        {
            var argScore = ScoreArgumentType(sourceTypes[i], elementType);
            if (argScore < 0)
                return false;

            runningScore += argScore;
            conversionBuilder.Add(new BoundConversionPlan(
                sourceTypes[i],
                elementType,
                sourceTypes[i] == elementType));
        }

        bindingBuilder.Add(new BoundParameterBinding(
            BoundParameterBindingKind.ParamsArray,
            lastParameterIndex,
            SourceArgumentIndex: lastParameterIndex,
            SourceArgumentCount: sourceTypes.Count - lastParameterIndex));

        conversions = conversionBuilder.ToImmutable();
        bindings = bindingBuilder.ToImmutable();
        score = runningScore - (defaultsUsed * DefaultArgumentPenalty);
        return true;
    }

    private static Type? GetNormalFormTargetType(
        ParameterInfo[] parameters,
        int parameterIndex,
        bool hasParams,
        bool sourceCountMatchesParameterCount)
    {
        if (parameterIndex >= parameters.Length)
            return null;

        if (hasParams && parameterIndex == parameters.Length - 1 && !sourceCountMatchesParameterCount)
            return null;

        return GetParameterTargetType(parameters[parameterIndex]);
    }

    private static Type GetParameterTargetType(ParameterInfo parameter)
    {
        var parameterType = parameter.ParameterType;
        return parameterType.IsByRef
            ? parameterType.GetElementType() ?? typeof(object)
            : parameterType;
    }

    private static int ScoreArgumentType(Type sourceType, Type targetType)
    {
        if (sourceType == targetType)
            return ExactMatchScore;

        if (targetType.IsAssignableFrom(sourceType))
            return AssignableMatchScore;

        if (TypeHelpers.CanImplicitlyConvert(sourceType, targetType))
            return ImplicitConversionScore;

        return -1;
    }

    private static int CompareMethodSpecificity(MethodInfo left, MethodInfo right)
    {
        var leftParameters = MethodDispatchCache.GetParameters(left);
        var rightParameters = MethodDispatchCache.GetParameters(right);

        var leftIsParams = IsParamsMethod(leftParameters);
        var rightIsParams = IsParamsMethod(rightParameters);
        if (leftIsParams != rightIsParams)
            return leftIsParams ? -1 : 1;

        if (left.IsGenericMethod != right.IsGenericMethod)
            return left.IsGenericMethod ? -1 : 1;

        if (leftParameters.Length != rightParameters.Length)
            return leftParameters.Length < rightParameters.Length ? 1 : -1;

        var leftBetter = false;
        var rightBetter = false;
        var length = Math.Min(leftParameters.Length, rightParameters.Length);

        for (var i = 0; i < length; i++)
        {
            var leftType = GetSpecificityParameterType(leftParameters, i);
            var rightType = GetSpecificityParameterType(rightParameters, i);
            if (leftType == rightType)
                continue;

            // ECMA-334 §12.6.4.7: better conversion target
            var cmp = TypeHelpers.CompareBetterConversionTarget(leftType, rightType);
            if (cmp > 0)
                leftBetter = true;
            else if (cmp < 0)
                rightBetter = true;
        }

        return (leftBetter, rightBetter) switch
        {
            (true, false) => 1,
            (false, true) => -1,
            _ => 0
        };
    }

    private static Type GetSpecificityParameterType(ParameterInfo[] parameters, int index)
    {
        var parameterType = parameters[index].ParameterType;
        if (index == parameters.Length - 1 && parameters[index].IsDefined(typeof(ParamArrayAttribute), false))
            return parameterType.GetElementType() ?? parameterType;
        return parameterType;
    }

    private static bool IsParamsMethod(ParameterInfo[] parameters)
    {
        return parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute), false);
    }
}
