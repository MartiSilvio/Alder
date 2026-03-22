namespace Alder.Runtime;

internal static class MethodResolver
{
    public static MethodInfo? TryResolveMethod(MethodInfo[] methods, Type[] argTypes)
    {
        // Phase 1: filter to applicable candidates
        var applicable = new List<(MethodInfo Method, ParameterInfo[] Params, ApplicableForm Form)>();
        foreach (var method in methods)
        {
            if (method.ContainsGenericParameters)
                continue;

            var parameters = method.GetParameters();
            if (OverloadResolution.IsApplicable(parameters, argTypes, out var form))
                applicable.Add((method, parameters, form));
        }

        if (applicable.Count == 0)
            return null;
        if (applicable.Count == 1)
            return applicable[0].Method;

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
                    return null;
            }
        }

        return best.Method;
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
}
