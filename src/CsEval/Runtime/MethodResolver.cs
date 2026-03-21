namespace CsEval.Runtime;

internal static class MethodResolver
{
    public static MethodInfo? TryResolveMethod(MethodInfo[] methods, Type[] argTypes)
    {
        MethodInfo? best = null;
        var bestScore = -1;
        var ambiguous = false;

        foreach (var method in methods)
        {
            if (method.ContainsGenericParameters)
                continue;

            var parameters = method.GetParameters();
            var score = ScoreMethodByTypes(parameters, argTypes);

            if (score > bestScore)
            {
                bestScore = score;
                best = method;
                ambiguous = false;
            }
            else if (score >= 0 && score == bestScore)
            {
                ambiguous = true;
            }
        }

        return ambiguous ? null : best;
    }

    public static MethodInfo? TryResolveMethod(Type targetType, string methodName, Type[] argTypes, BindingFlags flags)
    {
        var comparison = (flags & BindingFlags.IgnoreCase) != 0
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var methods = ReflectionRuntime.GetMethods(targetType, flags)
            .Where(m => string.Equals(m.Name, methodName, comparison) && !m.ContainsGenericParameters)
            .ToArray();
        return TryResolveMethod(methods, argTypes);
    }

    private static int ScoreMethodByTypes(ParameterInfo[] parameters, Type[] argTypes)
    {
        if (parameters.Length != argTypes.Length)
        {
            if (parameters.Length == 0 || !parameters[^1].IsDefined(typeof(ParamArrayAttribute), false))
                return -1;
            return ScoreExpandedForm(parameters, argTypes);
        }

        var score = ScoreNormalForm(parameters, argTypes);
        if (score >= 0)
            return OverloadScoring.NormalFormBase + score;

        if (parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute), false))
        {
            var expandedScore = ScoreExpandedForm(parameters, argTypes);
            if (expandedScore >= 0)
                return expandedScore;
        }

        return -1;
    }

    private static int ScoreNormalForm(ParameterInfo[] parameters, Type[] argTypes)
    {
        var score = 0;
        for (var i = 0; i < argTypes.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            var argType = argTypes[i];

            if (argType == paramType)
                score += OverloadScoring.ExactMatch;
            else if (paramType.IsAssignableFrom(argType))
                score += OverloadScoring.AssignableMatch;
            else if (TypeHelpers.CanImplicitlyConvert(argType, paramType))
                score += OverloadScoring.ImplicitConversion;
            else
                return -1;
        }
        return score;
    }

    private static int ScoreExpandedForm(ParameterInfo[] parameters, Type[] argTypes)
    {
        var lastParamIndex = parameters.Length - 1;
        if (argTypes.Length < lastParamIndex)
            return -1;

        var elementType = parameters[lastParamIndex].ParameterType.GetElementType();
        if (elementType == null)
            return -1;

        var score = 0;
        for (var i = 0; i < lastParamIndex; i++)
        {
            var paramType = parameters[i].ParameterType;
            var argType = argTypes[i];

            if (argType == paramType)
                score += OverloadScoring.ExactMatch;
            else if (paramType.IsAssignableFrom(argType))
                score += OverloadScoring.AssignableMatch;
            else if (TypeHelpers.CanImplicitlyConvert(argType, paramType))
                score += OverloadScoring.ImplicitConversion;
            else
                return -1;
        }

        for (var i = lastParamIndex; i < argTypes.Length; i++)
        {
            if (argTypes[i] == elementType)
                score += OverloadScoring.ExactMatch;
            else if (elementType.IsAssignableFrom(argTypes[i]))
                score += OverloadScoring.AssignableMatch;
            else if (TypeHelpers.CanImplicitlyConvert(argTypes[i], elementType))
                score += OverloadScoring.ImplicitConversion;
            else
                return -1;
        }

        return OverloadScoring.ExpandedFormBase + score;
    }
}
