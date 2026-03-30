using System.Collections.Immutable;

namespace Alder.Runtime;

internal static class ArgumentPreparer
{
    public static object?[] Prepare(
        ResolvedCall resolved,
        object?[] args,
        ParameterInfo[] parameters,
        CancellationToken ct)
    {
        var sources = resolved.ArgMap.Sources;

        if (IsDirectMapping(resolved, args, parameters))
            return args;

        var prepared = new object?[parameters.Length];

        for (var paramIdx = 0; paramIdx < sources.Length && paramIdx < parameters.Length; paramIdx++)
        {
            var source = sources[paramIdx];
            switch (source.Kind)
            {
                case ParameterSourceKind.Argument:
                {
                    var argIdx = source.ArgumentIndex;
                    if ((uint)argIdx >= (uint)args.Length)
                        break;
                    prepared[paramIdx] = PrepareArgument(args[argIdx], resolved.Conversions, argIdx, parameters[paramIdx]);
                    break;
                }
                case ParameterSourceKind.Default:
                    prepared[paramIdx] = MaterializeDefault(parameters[paramIdx]);
                    break;
                case ParameterSourceKind.ParamsRange:
                {
                    var elementType = parameters[paramIdx].ParameterType.GetElementType()!;
                    prepared[paramIdx] = BuildParamsArray(
                        args, source.ParamsStartIndex, source.ParamsCount,
                        elementType, resolved.Conversions);
                    break;
                }
            }
        }

        return prepared;
    }

    public static object?[] Prepare(
        ResolvedConstructorCall resolved,
        object?[] args,
        ParameterInfo[] parameters)
    {
        var sources = resolved.ArgMap.Sources;

        if (IsDirectMapping(resolved.ArgMap, resolved.Form, resolved.Conversions, args, parameters))
            return args;

        var prepared = new object?[parameters.Length];

        for (var paramIdx = 0; paramIdx < sources.Length && paramIdx < parameters.Length; paramIdx++)
        {
            var source = sources[paramIdx];
            switch (source.Kind)
            {
                case ParameterSourceKind.Argument:
                {
                    var argIdx = source.ArgumentIndex;
                    if ((uint)argIdx >= (uint)args.Length)
                        break;
                    prepared[paramIdx] = PrepareArgument(args[argIdx], resolved.Conversions, argIdx, parameters[paramIdx]);
                    break;
                }
                case ParameterSourceKind.Default:
                    prepared[paramIdx] = MaterializeDefault(parameters[paramIdx]);
                    break;
                case ParameterSourceKind.ParamsRange:
                {
                    var elementType = parameters[paramIdx].ParameterType.GetElementType()!;
                    prepared[paramIdx] = BuildParamsArray(
                        args, source.ParamsStartIndex, source.ParamsCount,
                        elementType, resolved.Conversions);
                    break;
                }
            }
        }

        return prepared;
    }

    public static bool IsDirectMapping(
        ResolvedCall resolved,
        object?[] args,
        ParameterInfo[] parameters) =>
        IsDirectMapping(resolved.ArgMap, resolved.Form, resolved.Conversions, args, parameters);

    private static bool IsDirectMapping(
        ArgumentParameterMap argMap,
        ApplicableForm form,
        ImmutableArray<ArgumentConversion> conversions,
        object?[] args,
        ParameterInfo[] parameters)
    {
        if (args.Length != parameters.Length)
            return false;
        if (form != ApplicableForm.Normal)
            return false;

        var sources = argMap.Sources;
        if (sources.Length != parameters.Length)
            return false;

        for (var i = 0; i < sources.Length; i++)
        {
            if (sources[i].Kind != ParameterSourceKind.Argument || sources[i].ArgumentIndex != i)
                return false;
        }

        for (var i = 0; i < conversions.Length; i++)
        {
            if (conversions[i].Kind != ArgumentConversionKind.Identity)
                return false;
        }

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is NamedArg or OutArgMarker)
                return false;
        }

        return true;
    }

    public static object? ApplyConversion(object? value, ArgumentConversion conversion)
    {
        return conversion.Kind switch
        {
            ArgumentConversionKind.Identity => value,
            ArgumentConversionKind.ImplicitReference => value,
            ArgumentConversionKind.Boxing => value,
            ArgumentConversionKind.ImplicitNumeric => TypeHelpers.CoerceNumeric(value!, conversion.TargetType),
            ArgumentConversionKind.UserDefined => ApplyUserDefinedConversion(value!, conversion.TargetType),
            ArgumentConversionKind.LambdaToDelegate => LambdaDelegateConverter.TryConvert(value!, conversion.TargetType),
            _ => value
        };
    }

    internal static void CopyBackOutArgs(object?[] originalArgs, object?[] convertedArgs, ParameterInfo[] parameters)
    {
        for (var i = 0; i < originalArgs.Length && i < parameters.Length; i++)
        {
            if (originalArgs[i] is OutArgMarker && parameters[i].ParameterType.IsByRef)
                originalArgs[i] = convertedArgs[i];
        }
    }

    internal static object? MaterializeDefault(ParameterInfo parameter)
    {
        if (parameter.IsDefined(typeof(ParamArrayAttribute), false))
            return RuntimeArrayFactory.Create(parameter.ParameterType.GetElementType()!, 0);

        var defaultValue = parameter.DefaultValue;
        if (defaultValue == Type.Missing || defaultValue == DBNull.Value)
            return TypeHelpers.GetDefaultValue(parameter.ParameterType);
        if (defaultValue == null &&
            parameter.ParameterType.IsValueType &&
            Nullable.GetUnderlyingType(parameter.ParameterType) == null)
            return TypeHelpers.GetDefaultValue(parameter.ParameterType);
        return defaultValue;
    }

    private static object? PrepareArgument(
        object? arg,
        ImmutableArray<ArgumentConversion> conversions,
        int argIdx,
        ParameterInfo parameter)
    {
        if (arg is NamedArg named)
            arg = named.Value;

        if (arg is OutArgMarker && parameter.ParameterType.IsByRef)
        {
            var elementType = parameter.ParameterType.GetElementType()!;
            return TypeHelpers.GetDefaultValue(elementType);
        }

        if ((uint)argIdx < (uint)conversions.Length)
            return ApplyConversion(arg, conversions[argIdx]);

        return arg;
    }

    private static Array BuildParamsArray(
        object?[] args,
        int startIndex,
        int count,
        Type elementType,
        ImmutableArray<ArgumentConversion> conversions)
    {
        var array = RuntimeArrayFactory.Create(elementType, count);
        for (var i = 0; i < count; i++)
        {
            var argIdx = startIndex + i;
            var arg = (uint)argIdx < (uint)args.Length ? args[argIdx] : null;
            if (arg is NamedArg named)
                arg = named.Value;

            var converted = (uint)argIdx < (uint)conversions.Length
                ? ApplyConversion(arg, conversions[argIdx])
                : arg;
            array.SetValue(converted, i);
        }
        return array;
    }

    private static object ApplyUserDefinedConversion(object value, Type targetType)
    {
        if (TypeHelpers.TryApplyUserDefinedImplicitConversion(value, targetType, out var result))
            return result!;

        return value;
    }
}
