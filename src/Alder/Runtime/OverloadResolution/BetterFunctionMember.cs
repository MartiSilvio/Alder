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

/// <summary>ECMA-334 §12.6.4 better-function-member comparison.</summary>
internal static class OverloadResolution
{
    public static BetterResult BetterFunctionMember(
        MethodCandidate left,
        MethodCandidate right,
        ReadOnlySpan<ArgumentDescriptor> args)
    {
        var leftBetter = false;
        var rightBetter = false;

        for (var i = 0; i < args.Length; i++)
        {
            var leftType = GetEffectiveParameterType(left.Parameters, i, left.Form);
            var rightType = GetEffectiveParameterType(right.Parameters, i, right.Form);

            if (leftType == rightType)
                continue;

            var cmp = BetterConversionFromExpression(args[i], i, left, right, leftType, rightType);
            if (cmp == BetterResult.Left)
                leftBetter = true;
            else if (cmp == BetterResult.Right)
                rightBetter = true;
        }

        if (leftBetter && !rightBetter) return BetterResult.Left;
        if (rightBetter && !leftBetter) return BetterResult.Right;

        return TieBreak(left, right);
    }

    private static BetterResult BetterConversionFromExpression(
        ArgumentDescriptor arg,
        int argIndex,
        MethodCandidate left,
        MethodCandidate right,
        Type t1,
        Type t2)
    {
        var exactLeft = ExactlyMatches(arg, argIndex, left, t1);
        var exactRight = ExactlyMatches(arg, argIndex, right, t2);

        if (exactLeft && !exactRight) return BetterResult.Left;
        if (exactRight && !exactLeft) return BetterResult.Right;

        var targetResult = BetterConversionTarget(t1, t2);
        if (targetResult != BetterResult.Neither)
            return targetResult;

        if (arg.Kind == ArgumentKind.Lambda)
            return BetterConversionFromLambda(argIndex, left, right, t1, t2);

        return BetterResult.Neither;
    }

    private static BetterResult BetterConversionFromLambda(
        int argIndex,
        MethodCandidate left,
        MethodCandidate right,
        Type leftDelegate,
        Type rightDelegate)
    {
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

        var inferredReturn = left.LambdaReturnTypes?[argIndex]
                          ?? right.LambdaReturnTypes?[argIndex];

        if (inferredReturn != null && inferredReturn != typeof(object))
        {
            var exactLeft = inferredReturn == leftReturn;
            var exactRight = inferredReturn == rightReturn;
            if (exactLeft && !exactRight) return BetterResult.Left;
            if (exactRight && !exactLeft) return BetterResult.Right;

            return BetterConversionTarget(leftReturn, rightReturn);
        }

        // §12.6.4.4: when the lambda return type can't be inferred, neither conversion is better
        return BetterResult.Neither;
    }

    private static bool ExactlyMatches(ArgumentDescriptor arg, int argIndex, MethodCandidate candidate, Type targetType)
    {
        if (arg.Kind == ArgumentKind.Value)
            return arg.StaticType == targetType;

        if (arg.Kind == ArgumentKind.Lambda)
            return LambdaExactlyMatchesDelegate(argIndex, candidate, targetType);

        return false;
    }

    private static bool LambdaExactlyMatchesDelegate(
        int argIndex,
        MethodCandidate candidate,
        Type delegateType)
    {
        var invoke = delegateType.GetMethod("Invoke");
        if (invoke == null)
            return false;

        var delegateReturnType = invoke.ReturnType;
        if (delegateReturnType == typeof(void))
            return false;

        var inferredReturn = candidate.LambdaReturnTypes?[argIndex];
        return inferredReturn != null && inferredReturn == delegateReturnType;
    }

    private static BetterResult BetterConversionTarget(Type t1, Type t2)
    {
        var cmp = TypeHelpers.CompareBetterConversionTarget(t1, t2);
        if (cmp > 0) return BetterResult.Left;
        if (cmp < 0) return BetterResult.Right;
        return BetterResult.Neither;
    }

    private static BetterResult TieBreak(MethodCandidate left, MethodCandidate right)
    {
        // Rule 1: Non-generic beats generic
        if (left.IsGeneric != right.IsGeneric)
            return left.IsGeneric ? BetterResult.Right : BetterResult.Left;

        // Rule 2: Normal form beats expanded form
        if (left.Form != right.Form)
            return left.Form == ApplicableForm.Normal ? BetterResult.Left : BetterResult.Right;

        // Rule 3: Fewer elements in expanded params (both expanded)
        if (left.Form == ApplicableForm.Expanded && right.Form == ApplicableForm.Expanded)
        {
            var leftParamsCount = left.ArgMap.ParamsParameterIndex >= 0
                ? left.ArgMap.Sources[left.ArgMap.ParamsParameterIndex].ParamsCount
                : 0;
            var rightParamsCount = right.ArgMap.ParamsParameterIndex >= 0
                ? right.ArgMap.Sources[right.ArgMap.ParamsParameterIndex].ParamsCount
                : 0;

            if (leftParamsCount != rightParamsCount)
                return leftParamsCount < rightParamsCount ? BetterResult.Left : BetterResult.Right;
        }

        // Rule 4: More specific parameter types (using uninstantiated types for generics)
        var specificityResult = CompareSpecificity(left, right);
        if (specificityResult != BetterResult.Neither)
            return specificityResult;

        // Fallback: parameter-by-parameter BetterConversionTarget for cases where
        // specificity is insufficient (e.g., string[] vs object[] params with 0 args)
        var paramConversionResult = CompareParameterConversionTargets(left, right);
        if (paramConversionResult != BetterResult.Neither)
            return paramConversionResult;

        // Rule 6: All params have arguments beats some needing defaults
        var leftDefaultCount = CountDefaults(left.ArgMap);
        var rightDefaultCount = CountDefaults(right.ArgMap);
        if (leftDefaultCount != rightDefaultCount)
            return leftDefaultCount < rightDefaultCount ? BetterResult.Left : BetterResult.Right;

        // Generic arity tiebreaker: fewer type parameters is more specific
        if (left.IsGeneric && right.IsGeneric)
        {
            var leftTypeParamCount = left.Method.GetGenericArguments().Length;
            var rightTypeParamCount = right.Method.GetGenericArguments().Length;
            if (leftTypeParamCount != rightTypeParamCount)
                return leftTypeParamCount < rightTypeParamCount ? BetterResult.Left : BetterResult.Right;
        }

        return BetterResult.Neither;
    }

    private static BetterResult CompareSpecificity(MethodCandidate left, MethodCandidate right)
    {
        var leftParams = left.UninstantiatedParameters;
        var rightParams = right.UninstantiatedParameters;
        var length = Math.Min(leftParams.Length, rightParams.Length);

        var leftMoreSpecific = false;
        var rightMoreSpecific = false;

        for (var i = 0; i < length; i++)
        {
            var leftType = GetSpecificityParameterType(leftParams, i);
            var rightType = GetSpecificityParameterType(rightParams, i);

            if (leftType == rightType)
                continue;

            var cmp = CompareTypeSpecificity(leftType, rightType);
            if (cmp > 0)
                leftMoreSpecific = true;
            else if (cmp < 0)
                rightMoreSpecific = true;
        }

        if (leftMoreSpecific && !rightMoreSpecific) return BetterResult.Left;
        if (rightMoreSpecific && !leftMoreSpecific) return BetterResult.Right;
        return BetterResult.Neither;
    }

    private static int CompareTypeSpecificity(Type left, Type right)
    {
        var leftIsParam = left.IsGenericParameter;
        var rightIsParam = right.IsGenericParameter;

        if (leftIsParam && !rightIsParam) return -1;
        if (!leftIsParam && rightIsParam) return 1;
        if (leftIsParam && rightIsParam) return 0;

        if (left.IsGenericType && right.IsGenericType &&
            left.GetGenericTypeDefinition() == right.GetGenericTypeDefinition())
        {
            var leftArgs = left.GetGenericArguments();
            var rightArgs = right.GetGenericArguments();

            var leftBetter = false;
            var rightBetter = false;

            for (var i = 0; i < leftArgs.Length && i < rightArgs.Length; i++)
            {
                var cmp = CompareTypeSpecificity(leftArgs[i], rightArgs[i]);
                if (cmp > 0) leftBetter = true;
                else if (cmp < 0) rightBetter = true;
            }

            if (leftBetter && !rightBetter) return 1;
            if (rightBetter && !leftBetter) return -1;
        }

        if (left.IsArray && right.IsArray && left.GetArrayRank() == right.GetArrayRank())
            return CompareTypeSpecificity(left.GetElementType()!, right.GetElementType()!);

        return 0;
    }

    private static BetterResult CompareParameterConversionTargets(
        MethodCandidate left, MethodCandidate right)
    {
        var leftParams = left.Parameters;
        var rightParams = right.Parameters;
        var length = Math.Min(leftParams.Length, rightParams.Length);

        var leftBetter = false;
        var rightBetter = false;

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

        if (leftBetter && !rightBetter) return BetterResult.Left;
        if (rightBetter && !leftBetter) return BetterResult.Right;
        return BetterResult.Neither;
    }

    private static int CountDefaults(ArgumentParameterMap argMap)
    {
        var count = 0;
        foreach (var source in argMap.Sources)
        {
            if (source.Kind == ParameterSourceKind.Default)
                count++;
        }
        return count;
    }

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
