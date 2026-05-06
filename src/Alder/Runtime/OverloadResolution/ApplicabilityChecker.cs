using System.Collections.Immutable;

namespace Alder.Runtime.OverloadResolution;

internal static class ApplicabilityChecker
{
    public static bool IsApplicable(
        ParameterInfo[] parameters,
        ReadOnlySpan<ArgumentDescriptor> args,
        out ApplicableForm form,
        out ArgumentParameterMap argMap,
        out ImmutableArray<ArgumentConversion> conversions)
    {
        form = ApplicableForm.Normal;
        argMap = ArgumentParameterMap.Empty;
        conversions = ImmutableArray<ArgumentConversion>.Empty;

        if (TryNormalForm(parameters, args, out argMap, out conversions))
            return true;

        if (parameters.Length > 0 &&
            parameters[^1].IsDefined(typeof(ParamArrayAttribute), false) &&
            TryExpandedForm(parameters, args, out argMap, out conversions))
        {
            form = ApplicableForm.Expanded;
            return true;
        }

        return false;
    }

    private static bool TryNormalForm(
        ParameterInfo[] parameters,
        ReadOnlySpan<ArgumentDescriptor> args,
        out ArgumentParameterMap argMap,
        out ImmutableArray<ArgumentConversion> conversions)
    {
        argMap = ArgumentParameterMap.Empty;
        conversions = ImmutableArray<ArgumentConversion>.Empty;

        var positionalCount = CountPositional(args);
        var namedCount = args.Length - positionalCount;

        if (positionalCount + namedCount > parameters.Length)
            return false;

        var sources = new ParameterSource[parameters.Length];
        var argConversions = new ArgumentConversion[args.Length];
        var filled = new bool[parameters.Length];
        var positionalIndex = 0;

        for (var paramIdx = 0; paramIdx < parameters.Length && positionalIndex < positionalCount; paramIdx++)
        {
            if (IsClaimedByNamedArg(parameters[paramIdx].Name!, args))
                continue;

            var argIdx = GetNthPositionalIndex(args, positionalIndex);
            if (!TryClassifyConversion(args[argIdx], parameters[paramIdx].ParameterType, out argConversions[argIdx]))
                return false;

            sources[paramIdx] = ParameterSource.FromArgument(argIdx);
            filled[paramIdx] = true;
            positionalIndex++;
        }

        if (positionalIndex < positionalCount)
            return false;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Name == null)
                continue;

            var paramIndex = FindParameterIndex(parameters, args[i].Name!);
            if (paramIndex < 0 || filled[paramIndex])
                return false;

            if (!TryClassifyConversion(args[i], parameters[paramIndex].ParameterType, out argConversions[i]))
                return false;

            sources[paramIndex] = ParameterSource.FromArgument(i);
            filled[paramIndex] = true;
        }

        for (var i = 0; i < parameters.Length; i++)
        {
            if (filled[i])
                continue;
            if (!parameters[i].HasDefaultValue &&
                !parameters[i].IsDefined(typeof(ParamArrayAttribute), false))
                return false;

            sources[i] = ParameterSource.FromDefault();
        }

        argMap = new ArgumentParameterMap(ImmutableArray.Create(sources), -1);
        conversions = ImmutableArray.Create(argConversions);
        return true;
    }

    private static bool TryExpandedForm(
        ParameterInfo[] parameters,
        ReadOnlySpan<ArgumentDescriptor> args,
        out ArgumentParameterMap argMap,
        out ImmutableArray<ArgumentConversion> conversions)
    {
        argMap = ArgumentParameterMap.Empty;
        conversions = ImmutableArray<ArgumentConversion>.Empty;

        var paramsIndex = parameters.Length - 1;
        var elementType = parameters[paramsIndex].ParameterType.GetElementType();
        if (elementType == null)
            return false;

        var sources = new ParameterSource[parameters.Length];
        var argConversions = new ArgumentConversion[args.Length];
        var filled = new bool[parameters.Length];
        var positionalIndex = 0;

        for (var paramIdx = 0; paramIdx < paramsIndex; paramIdx++)
        {
            if (IsClaimedByNamedArg(parameters[paramIdx].Name!, args))
                continue;

            if (positionalIndex >= CountPositional(args))
                break;

            var argIdx = GetNthPositionalIndex(args, positionalIndex);
            if (!TryClassifyConversion(args[argIdx], parameters[paramIdx].ParameterType, out argConversions[argIdx]))
                return false;

            sources[paramIdx] = ParameterSource.FromArgument(argIdx);
            filled[paramIdx] = true;
            positionalIndex++;
        }

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Name == null)
                continue;

            var paramIndex = FindParameterIndex(parameters, args[i].Name!);
            if (paramIndex < 0)
                return false;
            if (paramIndex == paramsIndex)
                continue;
            if (filled[paramIndex])
                return false;

            if (!TryClassifyConversion(args[i], parameters[paramIndex].ParameterType, out argConversions[i]))
                return false;

            sources[paramIndex] = ParameterSource.FromArgument(i);
            filled[paramIndex] = true;
        }

        for (var i = 0; i < paramsIndex; i++)
        {
            if (!filled[i] && !parameters[i].HasDefaultValue)
                return false;
            if (!filled[i])
                sources[i] = ParameterSource.FromDefault();
        }

        var totalPositional = CountPositional(args);
        var paramsCount = totalPositional - positionalIndex;

        for (var i = positionalIndex; i < totalPositional; i++)
        {
            var argIdx = GetNthPositionalIndex(args, i);
            if (!TryClassifyConversion(args[argIdx], elementType, out argConversions[argIdx]))
                return false;
        }

        sources[paramsIndex] = ParameterSource.FromParamsRange(positionalIndex, paramsCount);
        argMap = new ArgumentParameterMap(ImmutableArray.Create(sources), paramsIndex);
        conversions = ImmutableArray.Create(argConversions);
        return true;
    }

    private static bool TryClassifyConversion(
        ArgumentDescriptor arg,
        Type paramType,
        out ArgumentConversion conversion)
    {
        conversion = default;

        switch (arg.Kind)
        {
            case ArgumentKind.Null:
                if (paramType.IsValueType && Nullable.GetUnderlyingType(paramType) == null)
                    return false;
                conversion = ArgumentConversion.ForIdentity(paramType);
                return true;

            case ArgumentKind.Out:
                if (!paramType.IsByRef)
                    return false;
                conversion = ArgumentConversion.ForIdentity(paramType);
                return true;

            case ArgumentKind.Lambda:
            case ArgumentKind.MethodGroup:
                if (!typeof(Delegate).IsAssignableFrom(paramType))
                    return false;
                if (paramType.ContainsGenericParameters)
                {
                    // Open generic delegate: check arity only, type inference closes it later
                    if (!DelegateShapeInspector.TryGetInvoke(paramType, out var invoke))
                        return false;
                    if (invoke.GetParameters().Length != arg.LambdaArity)
                        return false;
                }
                else
                {
                    if (GetDelegateInputParameterCount(paramType) != arg.LambdaArity)
                        return false;
                }
                conversion = ArgumentConversion.ForLambdaToDelegate(paramType);
                return true;

            case ArgumentKind.Value:
                return TryClassifyValueConversion(arg.StaticType!, paramType, out conversion);

            default:
                return false;
        }
    }

    private static bool TryClassifyValueConversion(
        Type sourceType,
        Type paramType,
        out ArgumentConversion conversion)
    {
        conversion = default;

        if (sourceType == paramType)
        {
            conversion = ArgumentConversion.ForIdentity(paramType);
            return true;
        }

        if (paramType.IsAssignableFrom(sourceType))
        {
            if (sourceType.IsValueType && !paramType.IsValueType)
                conversion = ArgumentConversion.ForBoxing(paramType);
            else
                conversion = ArgumentConversion.ForImplicitReference(paramType);
            return true;
        }

        if (TypeHelpers.IsStandardImplicitConversion(sourceType, paramType))
        {
            conversion = ArgumentConversion.ForImplicitNumeric(paramType);
            return true;
        }

        if (TypeHelpers.HasUserDefinedImplicitConversion(sourceType, paramType))
        {
            conversion = ArgumentConversion.ForUserDefined(paramType);
            return true;
        }

        return false;
    }

    private static int GetDelegateInputParameterCount(Type delegateType)
    {
        return DelegateShapeInspector.GetInputParameterCountOrMinusOne(delegateType);
    }

    private static bool IsClaimedByNamedArg(string paramName, ReadOnlySpan<ArgumentDescriptor> args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i].Name, paramName, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static int FindParameterIndex(ParameterInfo[] parameters, string name)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            if (string.Equals(parameters[i].Name, name, StringComparison.Ordinal))
                return i;
        }
        return -1;
    }

    private static int CountPositional(ReadOnlySpan<ArgumentDescriptor> args)
    {
        var count = 0;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Name == null)
                count++;
        }
        return count;
    }

    private static int GetNthPositionalIndex(ReadOnlySpan<ArgumentDescriptor> args, int n)
    {
        var count = 0;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Name == null)
            {
                if (count == n)
                    return i;
                count++;
            }
        }
        return -1;
    }
}
