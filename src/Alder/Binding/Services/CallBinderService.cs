using System.Collections.Immutable;
using Alder.Binding.Plans;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder.Binding.Services;

internal sealed class CallBinderService
{

    private readonly AlderContext _context;

    public CallBinderService(AlderContext context)
    {
        _context = context;
    }

    public bool TryBindStaticCall(Type declaringType, string methodName, IReadOnlyList<object?> args, bool isCaseSensitive, out BoundCallPlan? plan)
    {
        var flags = BindingFlags.Public | BindingFlags.Static;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeMetadata.GetMethods(declaringType, methodName, flags);
        var sourceTypes = args.Select(static arg => arg?.GetType() ?? typeof(object)).ToArray();
        return TryBindFromTypes(methods, sourceTypes, isStaticCall: true, out plan, out _);
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

    public bool TryBindInstanceCall(Type targetType, string methodName, IReadOnlyList<object?> args, bool isCaseSensitive, out BoundCallPlan? plan)
    {
        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeMetadata.GetMethods(targetType, methodName, flags);
        var sourceTypes = args.Select(static arg => arg?.GetType() ?? typeof(object)).ToArray();
        return TryBindFromTypes(methods, sourceTypes, isStaticCall: false, out plan, out _);
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

    public bool TryBindStaticCall(
        Type declaringType,
        string methodName,
        IReadOnlyList<Type> argumentTypes,
        bool isCaseSensitive,
        out BoundCallPlan? plan)
    {
        var flags = BindingFlags.Public | BindingFlags.Static;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeMetadata.GetMethods(declaringType, methodName, flags);
        return TryBindFromTypes(methods, argumentTypes, isStaticCall: true, out plan, out _);
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

    public bool TryBindInstanceCall(
        Type targetType,
        string methodName,
        IReadOnlyList<Type> argumentTypes,
        bool isCaseSensitive,
        out BoundCallPlan? plan)
    {
        var flags = BindingFlags.Public | BindingFlags.Instance;
        if (!isCaseSensitive)
            flags |= BindingFlags.IgnoreCase;

        var methods = _context.TypeMetadata.GetMethods(targetType, methodName, flags);
        return TryBindFromTypes(methods, argumentTypes, isStaticCall: false, out plan, out _);
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

    private static bool TryBindFromTypes(
        MethodInfo[] methods,
        IReadOnlyList<Type> sourceTypes,
        bool isStaticCall,
        out BoundCallPlan? plan,
        out bool isAmbiguous)
    {
        plan = null;
        isAmbiguous = false;

        if (methods.Length == 0)
            return false;

        if (methods.Length > 1 && sourceTypes.Any(static sourceType => sourceType == typeof(object)))
            return false;

        // Phase 1: collect applicable candidates with their plans
        var applicable = new List<(BoundCallPlan Plan, MethodInfo Method, ParameterInfo[] Params, ApplicableForm Form)>();
        var argTypes = sourceTypes is Type[] arr ? arr : sourceTypes.ToArray();

        foreach (var method in methods)
        {
            if (!TryCreatePlan(method, sourceTypes, isStaticCall, out var candidatePlan))
                continue;

            var parameters = MethodDispatchCache.GetParameters(method);
            if (OverloadResolution.IsApplicable(parameters, argTypes, out var form))
                applicable.Add((candidatePlan, method, parameters, form));
        }

        if (applicable.Count == 0)
            return false;

        if (applicable.Count == 1)
        {
            plan = applicable[0].Plan;
            return true;
        }

        // Phase 2: pairwise elimination
        var best = applicable[0];
        var bestIsUnique = true;

        for (var i = 1; i < applicable.Count; i++)
        {
            var challenger = applicable[i];
            var cmp = OverloadResolution.BetterFunctionMember(
                best.Method, best.Params, best.Form,
                challenger.Method, challenger.Params, challenger.Form,
                argTypes);

            switch (cmp)
            {
                case BetterResult.Right:
                    best = challenger;
                    bestIsUnique = true;
                    break;
                case BetterResult.Neither:
                    bestIsUnique = false;
                    break;
            }
        }

        // Phase 3: verify winner beats all
        if (!bestIsUnique)
        {
            foreach (var candidate in applicable)
            {
                if (candidate.Method == best.Method)
                    continue;
                var cmp = OverloadResolution.BetterFunctionMember(
                    best.Method, best.Params, best.Form,
                    candidate.Method, candidate.Params, candidate.Form,
                    argTypes);
                if (cmp != BetterResult.Left)
                {
                    isAmbiguous = true;
                    return false;
                }
            }
        }

        plan = best.Plan;
        return true;
    }

    private static BoundCallPlan BindFromTypes(
        MethodInfo[] methods,
        IReadOnlyList<Type> sourceTypes,
        string methodName,
        bool isStaticCall)
    {
        if (methods.Length == 0)
            throw new AlderException(DiagnosticDescriptors.MemberNotFound, "type", methodName);

        if (methods.Length > 1 && sourceTypes.Any(static sourceType => sourceType == typeof(object)))
            throw new AlderException(DiagnosticDescriptors.RuntimeOverloadResolutionRequired, methodName);

        if (TryBindFromTypes(methods, sourceTypes, isStaticCall, out var plan, out var isAmbiguous))
            return plan!;

        if (isAmbiguous)
            throw new AlderException(DiagnosticDescriptors.AmbiguousMethodInvocation, methodName);

        throw new AlderException(DiagnosticDescriptors.NoApplicableOverload, methodName);
    }

    private static bool TryCreatePlan(
        MethodInfo selectedMethod,
        IReadOnlyList<Type> sourceTypes,
        bool isStaticCall,
        out BoundCallPlan plan)
    {
        plan = null!;

        if (selectedMethod.ContainsGenericParameters)
            return false;

        var parameters = MethodDispatchCache.GetParameters(selectedMethod);
        if (parameters.Any(static parameter => parameter.ParameterType.IsByRef))
            return false;

        var normalOk = TryBuildNormalPlan(
            parameters, sourceTypes,
            out var normalConversions, out var normalBindings, out var normalDirectMapping);

        var expandedOk = false;
        var expandedDirectMapping = false;
        ImmutableArray<BoundConversionPlan> expandedConversions = default;
        ImmutableArray<BoundParameterBinding> expandedBindings = default;

        if (!normalOk)
        {
            expandedOk = TryBuildExpandedParamsPlan(
                parameters, sourceTypes,
                out expandedConversions, out expandedBindings, out expandedDirectMapping);
        }

        if (!normalOk && !expandedOk)
            return false;

        // Normal form is preferred over expanded form per ECMA-334 §12.6.4.3
        var useExpanded = !normalOk;
        var selectedConversions = useExpanded ? expandedConversions : normalConversions;
        var selectedBindings = useExpanded ? expandedBindings : normalBindings;
        var isDirectArgumentMapping = useExpanded ? expandedDirectMapping : normalDirectMapping;

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
            isDirectArgumentMapping = false;
            return false;
        }

        var conversionBuilder = ImmutableArray.CreateBuilder<BoundConversionPlan>(sourceCount);
        var bindingBuilder = ImmutableArray.CreateBuilder<BoundParameterBinding>(parameterCount);

        for (var i = 0; i < sourceCount; i++)
        {
            var targetType = GetNormalFormTargetType(parameters, i, hasParams, sourceCount == parameterCount);
            if (targetType == null)
            {
                conversions = default;
                bindings = default;
                isDirectArgumentMapping = false;
                return false;
            }

            if (!IsTypeApplicable(sourceTypes[i], targetType))
            {
                conversions = default;
                bindings = default;
                isDirectArgumentMapping = false;
                return false;
            }

            if (sourceTypes[i] != targetType)
                isDirectArgumentMapping = false;

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
                isDirectArgumentMapping = false;
                return false;
            }

            isDirectArgumentMapping = false;
            bindingBuilder.Add(new BoundParameterBinding(
                BoundParameterBindingKind.DefaultValue,
                i,
                SourceArgumentIndex: -1,
                SourceArgumentCount: 0));
        }

        conversions = conversionBuilder.ToImmutable();
        bindings = bindingBuilder.ToImmutable();
        return true;
    }

    private static bool TryBuildExpandedParamsPlan(
        ParameterInfo[] parameters,
        IReadOnlyList<Type> sourceTypes,
        out ImmutableArray<BoundConversionPlan> conversions,
        out ImmutableArray<BoundParameterBinding> bindings,
        out bool isDirectArgumentMapping)
    {
        conversions = default;
        bindings = default;
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

        for (var i = 0; i < lastParameterIndex; i++)
        {
            if (i < sourceTypes.Count)
            {
                var targetType = GetParameterTargetType(parameters[i]);
                if (!IsTypeApplicable(sourceTypes[i], targetType))
                    return false;

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

            bindingBuilder.Add(new BoundParameterBinding(
                BoundParameterBindingKind.DefaultValue,
                i,
                SourceArgumentIndex: -1,
                SourceArgumentCount: 0));
        }

        for (var i = lastParameterIndex; i < sourceTypes.Count; i++)
        {
            if (!IsTypeApplicable(sourceTypes[i], elementType))
                return false;

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

    private static bool IsTypeApplicable(Type sourceType, Type targetType)
    {
        if (sourceType == targetType)
            return true;
        if (targetType.IsAssignableFrom(sourceType))
            return true;
        if (TypeHelpers.CanImplicitlyConvert(sourceType, targetType))
            return true;
        if (TypeHelpers.HasUserDefinedImplicitConversion(sourceType, targetType))
            return true;
        return false;
    }
}
