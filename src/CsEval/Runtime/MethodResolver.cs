using System.Reflection;

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
            return -1;
        }

        var score = 0;
        for (var i = 0; i < argTypes.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            var argType = argTypes[i];

            if (argType == paramType)
            {
                score += 100;
            }
            else if (paramType.IsAssignableFrom(argType))
            {
                score += 10;
            }
            else if (TypeHelpers.CanImplicitlyConvert(argType, paramType))
            {
                score += 1;
            }
            else
            {
                return -1;
            }
        }
        return score;
    }
}
