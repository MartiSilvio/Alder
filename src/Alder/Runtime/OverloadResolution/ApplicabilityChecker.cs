using System.Collections.Immutable;
using System.Reflection;

namespace Alder.Runtime;

internal static class ApplicabilityChecker
{
    public static bool IsApplicable(
        ParameterInfo[] parameters,
        ReadOnlySpan<ArgumentDescriptor> args,
        out ApplicableForm form,
        out ArgumentParameterMap argMap)
    {
        form = ApplicableForm.Normal;
        argMap = ArgumentParameterMap.Empty;

        if (TryNormalForm(parameters, args, out argMap))
            return true;

        if (parameters.Length > 0 &&
            parameters[^1].IsDefined(typeof(ParamArrayAttribute), false) &&
            TryExpandedForm(parameters, args, out argMap))
        {
            form = ApplicableForm.Expanded;
            return true;
        }

        return false;
    }

    private static bool TryNormalForm(
        ParameterInfo[] parameters,
        ReadOnlySpan<ArgumentDescriptor> args,
        out ArgumentParameterMap argMap)
    {
        argMap = ArgumentParameterMap.Empty;

        var positionalCount = 0;
        var namedCount = 0;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Name != null)
                namedCount++;
            else
                positionalCount++;
        }

        if (positionalCount + namedCount > parameters.Length)
            return false;

        var sources = new ParameterSource[parameters.Length];
        var filled = new bool[parameters.Length];
        var positionalIndex = 0;

        for (var paramIdx = 0; paramIdx < parameters.Length && positionalIndex < positionalCount; paramIdx++)
        {
            if (IsClaimedByNamedArg(parameters[paramIdx].Name!, args))
                continue;

            var argIdx = GetNthPositionalIndex(args, positionalIndex);
            if (!IsDescriptorCompatible(args[argIdx], parameters[paramIdx].ParameterType))
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

            if (!IsDescriptorCompatible(args[i], parameters[paramIndex].ParameterType))
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
        return true;
    }

    private static bool TryExpandedForm(
        ParameterInfo[] parameters,
        ReadOnlySpan<ArgumentDescriptor> args,
        out ArgumentParameterMap argMap)
    {
        argMap = ArgumentParameterMap.Empty;

        var paramsIndex = parameters.Length - 1;
        var elementType = parameters[paramsIndex].ParameterType.GetElementType();
        if (elementType == null)
            return false;

        var sources = new ParameterSource[parameters.Length];
        var filled = new bool[parameters.Length];
        var positionalIndex = 0;

        for (var paramIdx = 0; paramIdx < paramsIndex; paramIdx++)
        {
            if (IsClaimedByNamedArg(parameters[paramIdx].Name!, args))
                continue;

            if (positionalIndex >= CountPositional(args))
                break;

            var argIdx = GetNthPositionalIndex(args, positionalIndex);
            if (!IsDescriptorCompatible(args[argIdx], parameters[paramIdx].ParameterType))
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

            if (!IsDescriptorCompatible(args[i], parameters[paramIndex].ParameterType))
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

        var paramsStartArgIndex = positionalIndex;
        var totalPositional = CountPositional(args);
        var paramsCount = totalPositional - positionalIndex;

        for (var i = positionalIndex; i < totalPositional; i++)
        {
            var argIdx = GetNthPositionalIndex(args, i);
            if (!IsDescriptorCompatible(args[argIdx], elementType))
                return false;
        }

        sources[paramsIndex] = ParameterSource.FromParamsRange(paramsStartArgIndex, paramsCount);
        argMap = new ArgumentParameterMap(ImmutableArray.Create(sources), paramsIndex);
        return true;
    }

    private static bool IsDescriptorCompatible(ArgumentDescriptor arg, Type paramType)
    {
        return arg.Kind switch
        {
            ArgumentKind.Null => !paramType.IsValueType || Nullable.GetUnderlyingType(paramType) != null,
            ArgumentKind.Out => paramType.IsByRef,
            ArgumentKind.Lambda => LambdaDelegateConverter.IsSupportedDelegateType(paramType) &&
                                   GetDelegateInputParameterCount(paramType) == arg.LambdaArity,
            ArgumentKind.Value => TypeHelpers.CanImplicitlyConvert(arg.StaticType!, paramType),
            _ => false
        };
    }

    private static int GetDelegateInputParameterCount(Type delegateType)
    {
        var invoke = delegateType.GetMethod("Invoke");
        if (invoke == null)
            return -1;

        var invokeParams = invoke.GetParameters();
        return invokeParams.Length;
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
