namespace Alder.Runtime;

internal enum ApplicableForm : byte
{
    Normal,
    Expanded
}

internal enum BetterResult : byte
{
    Left,
    Right,
    Neither
}

/// <summary>
/// ECMA-334 §12.6.4 overload resolution via pairwise elimination.
/// </summary>
internal static class OverloadResolution
{
    /// <summary>
    /// Tests whether a method is applicable for the given argument types.
    /// Tries normal form first, then expanded params form per §12.6.4.2.
    /// </summary>
    public static bool IsApplicable(ParameterInfo[] parameters, Type[] argTypes, out ApplicableForm form)
    {
        form = ApplicableForm.Normal;

        if (IsApplicableNormalForm(parameters, argTypes))
            return true;

        if (parameters.Length > 0 &&
            parameters[^1].IsDefined(typeof(ParamArrayAttribute), false) &&
            IsApplicableExpandedForm(parameters, argTypes))
        {
            form = ApplicableForm.Expanded;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tests whether a method is applicable for runtime args (object?[]) which may include
    /// named args, null values, out markers, and lambda-to-delegate conversions.
    /// </summary>
    public static bool IsApplicableForArgs(ParameterInfo[] parameters, object?[] args, out ApplicableForm form)
    {
        form = ApplicableForm.Normal;

        if (IsApplicableNormalFormForArgs(parameters, args))
            return true;

        if (parameters.Length > 0 &&
            parameters[^1].IsDefined(typeof(ParamArrayAttribute), false) &&
            IsApplicableExpandedFormForArgs(parameters, args))
        {
            form = ApplicableForm.Expanded;
            return true;
        }

        return false;
    }

    /// <summary>
    /// ECMA-334 §12.6.4.3: determines which of two applicable methods is better.
    /// Compares argument-by-argument, then applies tie-breaking rules.
    /// </summary>
    public static BetterResult BetterFunctionMember(
        MethodInfo left, ParameterInfo[] leftParams, ApplicableForm leftForm,
        MethodInfo right, ParameterInfo[] rightParams, ApplicableForm rightForm,
        Type[] argTypes)
    {
        var leftBetter = false;
        var rightBetter = false;

        var count = argTypes.Length;
        for (var i = 0; i < count; i++)
        {
            var leftType = GetEffectiveParameterType(leftParams, i, leftForm);
            var rightType = GetEffectiveParameterType(rightParams, i, rightForm);

            if (leftType == rightType)
                continue;

            var cmp = BetterConversionFromType(argTypes[i], leftType, rightType);
            if (cmp == BetterResult.Left)
                leftBetter = true;
            else if (cmp == BetterResult.Right)
                rightBetter = true;
        }

        if (leftBetter && !rightBetter) return BetterResult.Left;
        if (rightBetter && !leftBetter) return BetterResult.Right;

        return TieBreak(left, leftParams, leftForm, right, rightParams, rightForm);
    }

    /// <summary>
    /// Overload for runtime args where argument types must be extracted from object?[] values.
    /// </summary>
    public static BetterResult BetterFunctionMemberForArgs(
        MethodInfo left, ParameterInfo[] leftParams, ApplicableForm leftForm,
        MethodInfo right, ParameterInfo[] rightParams, ApplicableForm rightForm,
        object?[] args,
        AlderContext? context = null)
    {
        var leftBetter = false;
        var rightBetter = false;

        var count = args.Length;
        for (var i = 0; i < count; i++)
        {
            var arg = args[i];
            if (arg is NamedArg)
                continue;

            var leftType = GetEffectiveParameterType(leftParams, i, leftForm);
            var rightType = GetEffectiveParameterType(rightParams, i, rightForm);

            if (leftType == rightType)
                continue;

            // §12.6.4.4: Better conversion from expression — for lambda arguments,
            // compare delegate return types using the lambda's inferred return type.
            if (arg is LambdaValue or CompiledLambdaValue)
            {
                var cmp = BetterConversionFromLambda(arg, leftType, rightType, context);
                if (cmp == BetterResult.Left) leftBetter = true;
                else if (cmp == BetterResult.Right) rightBetter = true;
                continue;
            }

            var argType = arg?.GetType();
            if (argType == null)
                continue;

            var cmp2 = BetterConversionFromType(argType, leftType, rightType);
            if (cmp2 == BetterResult.Left)
                leftBetter = true;
            else if (cmp2 == BetterResult.Right)
                rightBetter = true;
        }

        if (leftBetter && !rightBetter) return BetterResult.Left;
        if (rightBetter && !leftBetter) return BetterResult.Right;

        return TieBreak(left, leftParams, leftForm, right, rightParams, rightForm);
    }

    /// <summary>
    /// §12.6.4.4: Better conversion from expression for lambda arguments.
    /// When both target types are delegates with identical parameter lists,
    /// determines the lambda's return type and picks the delegate whose return type
    /// is a better conversion target.
    ///
    /// Two-tier strategy:
    /// 1. Static inference — bind the lambda body with parameter types to infer the return type.
    /// 2. Sample evaluation — if static inference is inconclusive (returns object or null),
    ///    evaluate the lambda once with a sample input from the collection to observe the
    ///    actual return type. This handles ExpandoObject properties, dynamic dispatch, and
    ///    any case where static analysis can't determine the type.
    /// </summary>
    private static BetterResult BetterConversionFromLambda(
        object arg, Type leftDelegate, Type rightDelegate, AlderContext? context)
    {
        var targetCmp = TypeHelpers.CompareBetterConversionTarget(leftDelegate, rightDelegate);
        if (targetCmp > 0) return BetterResult.Left;
        if (targetCmp < 0) return BetterResult.Right;

        var leftInvoke = leftDelegate.GetMethod("Invoke");
        var rightInvoke = rightDelegate.GetMethod("Invoke");
        if (leftInvoke == null || rightInvoke == null)
            return BetterResult.Neither;

        var leftReturn = leftInvoke.ReturnType;
        var rightReturn = rightInvoke.ReturnType;
        if (leftReturn == rightReturn)
            return BetterResult.Neither;

        var leftInputs = leftInvoke.GetParameters();
        var rightInputs = rightInvoke.GetParameters();
        if (leftInputs.Length != rightInputs.Length)
            return BetterResult.Neither;
        for (var i = 0; i < leftInputs.Length; i++)
        {
            if (leftInputs[i].ParameterType != rightInputs[i].ParameterType)
                return BetterResult.Neither;
        }

        var inputTypes = leftInputs.Select(static p => p.ParameterType).ToArray();

        // Tier 1: static inference
        var inferredReturn = ExtensionMethodResolver.InferLambdaReturnType(arg, inputTypes, context);
        if (inferredReturn != null && inferredReturn != typeof(object))
            return BetterConversionFromType(inferredReturn, leftReturn, rightReturn);

        // Tier 2: sample evaluation — invoke the lambda once to observe the actual return type
        var observedReturn = TryObserveLambdaReturnType(arg, inputTypes, context);
        if (observedReturn != null)
            return BetterConversionFromType(observedReturn, leftReturn, rightReturn);

        // If both tiers fail but we have object as inferred type, use it as a last resort
        // (picks the narrowest compatible overload)
        if (inferredReturn == typeof(object))
            return BetterConversionFromType(typeof(object), leftReturn, rightReturn);

        return BetterResult.Neither;
    }

    /// <summary>
    /// Evaluates a lambda with a sample input to observe its actual return type at runtime.
    /// Used when static type inference cannot determine the type (e.g., ExpandoObject properties).
    /// The sample value is set thread-locally by the extension method resolver before
    /// calling FindBestMethod, using the first element from the target collection.
    /// </summary>
    private static Type? TryObserveLambdaReturnType(object arg, Type[] inputTypes, AlderContext? context)
    {
        if (inputTypes.Length == 0)
            return null;

        var sample = CurrentSampleValue;
        if (sample == null)
            return null;

        try
        {
            object? result = arg switch
            {
                LambdaValue lambda => MethodInvoker.InvokeLambda(lambda, [sample], lambda.Closure),
                CompiledLambdaValue compiled => MethodInvoker.InvokeCompiledLambda(compiled, [sample]),
                _ => null
            };
            return result?.GetType();
        }
        catch
        {
            return null;
        }
    }

    [ThreadStatic]
    internal static object? CurrentSampleValue;

    /// <summary>
    /// ECMA-334 §12.6.4.6: better conversion from type S to T1 or T2.
    /// </summary>
    private static BetterResult BetterConversionFromType(Type argType, Type t1, Type t2)
    {
        if (argType == t1 && argType != t2) return BetterResult.Left;
        if (argType == t2 && argType != t1) return BetterResult.Right;

        var cmp = TypeHelpers.CompareBetterConversionTarget(t1, t2);
        if (cmp > 0) return BetterResult.Left;
        if (cmp < 0) return BetterResult.Right;
        return BetterResult.Neither;
    }

    /// <summary>
    /// Tie-breaking rules applied when argument-by-argument comparison is inconclusive.
    /// </summary>
    private static BetterResult TieBreak(
        MethodInfo left, ParameterInfo[] leftParams, ApplicableForm leftForm,
        MethodInfo right, ParameterInfo[] rightParams, ApplicableForm rightForm)
    {
        // §12.6.4.3: Normal form beats expanded form
        if (leftForm != rightForm)
            return leftForm == ApplicableForm.Normal ? BetterResult.Left : BetterResult.Right;

        // Non-generic beats generic
        if (left.IsGenericMethod != right.IsGenericMethod)
            return left.IsGenericMethod ? BetterResult.Right : BetterResult.Left;

        // Fewer parameters (fewer defaults used) is better
        if (leftParams.Length != rightParams.Length)
            return leftParams.Length < rightParams.Length ? BetterResult.Left : BetterResult.Right;

        // §12.6.4.7: better conversion target parameter-by-parameter
        var leftBetter = false;
        var rightBetter = false;
        var length = Math.Min(leftParams.Length, rightParams.Length);

        for (var i = 0; i < length; i++)
        {
            var leftType = GetSpecificityParameterType(leftParams, i);
            var rightType = GetSpecificityParameterType(rightParams, i);
            if (leftType == rightType)
                continue;

            var cmp = TypeHelpers.CompareBetterConversionTarget(leftType, rightType);
            if (cmp > 0)
                leftBetter = true;
            else if (cmp < 0)
                rightBetter = true;
        }

        return (leftBetter, rightBetter) switch
        {
            (true, false) => BetterResult.Left,
            (false, true) => BetterResult.Right,
            _ => BetterResult.Neither
        };
    }

    private static bool IsApplicableNormalForm(ParameterInfo[] parameters, Type[] argTypes)
    {
        if (argTypes.Length > parameters.Length)
            return false;

        for (var i = 0; i < argTypes.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            if (!TypeHelpers.CanImplicitlyConvert(argTypes[i], paramType))
                return false;
        }

        for (var i = argTypes.Length; i < parameters.Length; i++)
        {
            if (!parameters[i].HasDefaultValue &&
                !parameters[i].IsDefined(typeof(ParamArrayAttribute), false))
                return false;
        }

        return true;
    }

    private static bool IsApplicableExpandedForm(ParameterInfo[] parameters, Type[] argTypes)
    {
        var lastParamIndex = parameters.Length - 1;
        if (argTypes.Length < lastParamIndex)
            return false;

        var elementType = parameters[lastParamIndex].ParameterType.GetElementType();
        if (elementType == null)
            return false;

        for (var i = 0; i < lastParamIndex; i++)
        {
            if (i < argTypes.Length)
            {
                if (!TypeHelpers.CanImplicitlyConvert(argTypes[i], parameters[i].ParameterType))
                    return false;
            }
            else if (!parameters[i].HasDefaultValue)
            {
                return false;
            }
        }

        for (var i = lastParamIndex; i < argTypes.Length; i++)
        {
            if (!TypeHelpers.CanImplicitlyConvert(argTypes[i], elementType))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Mirrors the exact slot-skipping logic of MethodInvoker.TryBindNormalForm:
    /// positional args skip slots claimed by named args, named args are validated
    /// against already-filled slots, unfilled slots must have defaults.
    /// </summary>
    private static bool IsApplicableNormalFormForArgs(ParameterInfo[] parameters, object?[] args)
    {
        SplitArgs(args, out var positionalArgs, out var namedArgs);

        var filledParams = new bool[parameters.Length];
        var positionalIndex = 0;

        // Place positional args, skipping slots claimed by named args
        for (var i = 0; i < parameters.Length && positionalIndex < positionalArgs.Count; i++)
        {
            if (namedArgs.ContainsKey(parameters[i].Name!))
                continue;

            if (!IsArgConvertible(positionalArgs[positionalIndex++], parameters[i].ParameterType))
                return false;

            filledParams[i] = true;
        }

        // All positional args must have been placed
        if (positionalIndex < positionalArgs.Count)
            return false;

        // Validate named args: must map to a parameter, must not overlap with filled slots
        foreach (var (name, value) in namedArgs)
        {
            var paramIndex = FindParameterIndex(parameters, name);
            if (paramIndex < 0 || filledParams[paramIndex])
                return false;

            if (!IsArgConvertible(value, parameters[paramIndex].ParameterType))
                return false;

            filledParams[paramIndex] = true;
        }

        // Unfilled parameters must have defaults or be params
        for (var i = 0; i < parameters.Length; i++)
        {
            if (filledParams[i])
                continue;
            if (!parameters[i].HasDefaultValue &&
                !parameters[i].IsDefined(typeof(ParamArrayAttribute), false))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Mirrors the exact logic of MethodInvoker.TryBindExpandedParamsForm.
    /// </summary>
    private static bool IsApplicableExpandedFormForArgs(ParameterInfo[] parameters, object?[] args)
    {
        var paramsIndex = parameters.Length - 1;
        if (!parameters[paramsIndex].IsDefined(typeof(ParamArrayAttribute), false))
            return false;

        var elementType = parameters[paramsIndex].ParameterType.GetElementType();
        if (elementType == null)
            return false;

        SplitArgs(args, out var positionalArgs, out var namedArgs);

        var filledParams = new bool[parameters.Length];
        var positionalIndex = 0;

        // Place positional args into non-params slots, skipping named-claimed slots
        for (var i = 0; i < paramsIndex; i++)
        {
            if (namedArgs.ContainsKey(parameters[i].Name!))
                continue;

            if (positionalIndex >= positionalArgs.Count)
                break;

            if (!IsArgConvertible(positionalArgs[positionalIndex++], parameters[i].ParameterType))
                return false;

            filledParams[i] = true;
        }

        // Validate named args
        foreach (var (name, value) in namedArgs)
        {
            var paramIndex = FindParameterIndex(parameters, name);
            if (paramIndex < 0)
                return false;

            if (paramIndex == paramsIndex)
                continue; // params provided by name — handled separately

            if (filledParams[paramIndex])
                return false;

            if (!IsArgConvertible(value, parameters[paramIndex].ParameterType))
                return false;

            filledParams[paramIndex] = true;
        }

        // Unfilled non-params parameters must have defaults
        for (var i = 0; i < paramsIndex; i++)
        {
            if (!filledParams[i] && !parameters[i].HasDefaultValue)
                return false;
        }

        // Remaining positional args must be convertible to the params element type
        for (var i = positionalIndex; i < positionalArgs.Count; i++)
        {
            if (!IsArgConvertible(positionalArgs[i], elementType))
                return false;
        }

        return true;
    }

    private static void SplitArgs(object?[] args, out List<object?> positional, out Dictionary<string, object?> named)
    {
        positional = new List<object?>(args.Length);
        named = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var arg in args)
        {
            if (arg is NamedArg n)
                named[n.Name] = n.Value;
            else
                positional.Add(arg);
        }
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

    private static bool IsArgConvertible(object? arg, Type paramType)
    {
        if (arg is OutArgMarker)
            return paramType.IsByRef;

        if (arg == null)
            return !paramType.IsValueType || Nullable.GetUnderlyingType(paramType) != null;

        var argType = arg.GetType();
        if (TypeHelpers.CanImplicitlyConvert(argType, paramType))
            return true;

        if (arg is LambdaValue or CompiledLambdaValue && LambdaDelegateConverter.IsSupportedDelegateType(paramType))
            return LambdaDelegateConverter.TryConvert(arg, paramType) != null;

        return false;
    }

    /// <summary>
    /// Gets the effective parameter type for argument position i, accounting for params expansion.
    /// </summary>
    private static Type GetEffectiveParameterType(ParameterInfo[] parameters, int argIndex, ApplicableForm form)
    {
        if (form == ApplicableForm.Expanded)
        {
            var lastParamIndex = parameters.Length - 1;
            if (argIndex >= lastParamIndex)
                return parameters[lastParamIndex].ParameterType.GetElementType() ?? typeof(object);
        }

        if (argIndex < parameters.Length)
            return parameters[argIndex].ParameterType;

        return typeof(object);
    }

    private static Type GetSpecificityParameterType(ParameterInfo[] parameters, int index)
    {
        var parameterType = parameters[index].ParameterType;
        if (index == parameters.Length - 1 && parameters[index].IsDefined(typeof(ParamArrayAttribute), false))
            return parameterType.GetElementType() ?? parameterType;
        return parameterType;
    }
}
