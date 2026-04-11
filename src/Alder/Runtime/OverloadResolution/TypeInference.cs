namespace Alder.Runtime;

internal static class TypeInference
{
    /// <summary>
    /// Applies ECMA-334 method type inference to a generic method candidate.
    /// </summary>
    public static Type[]? Infer(MethodInfo genericMethod, Type?[] argTypes, object?[]? lambdaArgs, AlderContext? runtimeContext)
    {
        var genericParams = genericMethod.GetGenericArguments();
        var parameters = MethodDispatchCache.GetParameters(genericMethod);

        if (parameters.Length == 0 || argTypes.Length == 0)
            return null;

        var ctx = new InferenceContext(genericParams);

        // Phase 1 collects initial bounds from concrete arguments and explicit lambda parameter types.
        for (var i = 0; i < parameters.Length && i < argTypes.Length; i++)
        {
            if (argTypes[i] != null)
                LowerBoundInference(argTypes[i]!, parameters[i].ParameterType, ctx);

            if (lambdaArgs != null && i < lambdaArgs.Length && lambdaArgs[i] != null)
                ExplicitParameterTypeInference(lambdaArgs[i]!, parameters[i].ParameterType, ctx);
        }

        // Phase 2 fixes type parameters iteratively as more bounds become usable.
        IterativeFix(ctx, parameters, argTypes, lambdaArgs, runtimeContext);

        for (var i = 0; i < ctx.FixedTypes.Length; i++)
        {
            if (ctx.FixedTypes[i] == null)
                return null;
        }

        return RuntimeGenericFactory.TryCloseGenericMethod(genericMethod, ctx.FixedTypes!, out _)
            ? ctx.FixedTypes!
            : null;
    }

    private static bool IterativeFix(
        InferenceContext ctx,
        ParameterInfo[] parameters,
        Type?[] argTypes,
        object?[]? lambdaArgs,
        AlderContext? runtimeContext)
    {
        var maxIterations = ctx.GenericParams.Length * 2 + 1;

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            if (AllFixed(ctx))
                return true;

            var dependsOn = ComputeDependencies(ctx, parameters, lambdaArgs);

            // Prefer parameters whose remaining bounds no longer depend on other unfixed parameters.
            var fixedAny = false;
            for (var i = 0; i < ctx.GenericParams.Length; i++)
            {
                if (ctx.IsFixed[i])
                    continue;

                if (HasDependencyOnUnfixed(i, dependsOn, ctx))
                    continue;

                if (!HasAnyBounds(ctx, i))
                    continue;

                if (TryFixParam(ctx, i))
                    fixedAny = true;
            }

            if (!fixedAny)
            {
                // If that stalls, try parameters that already constrain other unfixed parameters.
                for (var i = 0; i < ctx.GenericParams.Length; i++)
                {
                    if (ctx.IsFixed[i])
                        continue;
                    if (!HasAnyBounds(ctx, i))
                        continue;
                    if (!IsDependedOnByOther(i, dependsOn, ctx))
                        continue;

                    if (TryFixParam(ctx, i))
                        fixedAny = true;
                }
            }

            if (!fixedAny)
            {
                // Last resort: attempt any parameter that has usable bounds.
                for (var i = 0; i < ctx.GenericParams.Length; i++)
                {
                    if (ctx.IsFixed[i])
                        continue;
                    if (!HasAnyBounds(ctx, i))
                        continue;
                    if (TryFixParam(ctx, i))
                        fixedAny = true;
                }
            }

            if (!fixedAny)
                return false;

            // Once lambda input types are fixed, use the lambda body to infer output bounds.
            if (lambdaArgs != null)
                PerformOutputTypeInference(ctx, parameters, argTypes, lambdaArgs, runtimeContext);
        }

        return AllFixed(ctx);
    }

    private static void PerformOutputTypeInference(
        InferenceContext ctx,
        ParameterInfo[] parameters,
        Type?[] argTypes,
        object?[] lambdaArgs,
        AlderContext? runtimeContext)
    {
        for (var i = 0; i < parameters.Length && i < lambdaArgs.Length; i++)
        {
            if (lambdaArgs[i] == null)
                continue;

            var paramType = parameters[i].ParameterType;
            if (!paramType.IsGenericType)
                continue;

            var paramGenericDef = paramType.GetGenericTypeDefinition();
            if (!IsFuncType(paramGenericDef))
                continue;

            var paramGenericArgs = paramType.GetGenericArguments();
            var inputTypes = new Type[paramGenericArgs.Length - 1];
            Array.Copy(paramGenericArgs, inputTypes, inputTypes.Length);

            if (!AllInputTypesFixed(inputTypes, ctx))
                continue;

            var substitutedInputTypes = SubstituteFixed(inputTypes, ctx);
            var boundInputTypes = Array.ConvertAll(substitutedInputTypes, static t => new Binding.BoundType(t));
            var resultType = ExtensionMethodResolver.InferLambdaReturnType(lambdaArgs[i], boundInputTypes, runtimeContext);
            if (resultType == null)
                continue;

            var outputType = paramGenericArgs[^1];
            LowerBoundInference(resultType, outputType, ctx);
        }
    }

    private static bool AllInputTypesFixed(Type[] inputTypes, InferenceContext ctx)
    {
        foreach (var t in inputTypes)
        {
            if (!t.IsGenericParameter)
                continue;

            var idx = ctx.IndexOf(t);
            if (idx >= 0 && !ctx.IsFixed[idx])
                return false;
        }
        return true;
    }

    private static Type[] SubstituteFixed(Type[] types, InferenceContext ctx)
    {
        var result = new Type[types.Length];
        for (var i = 0; i < types.Length; i++)
            result[i] = SubstituteSingle(types[i], ctx);
        return result;
    }

    private static Type SubstituteSingle(Type type, InferenceContext ctx)
    {
        if (type.IsGenericParameter)
        {
            var idx = ctx.IndexOf(type);
            if (idx >= 0 && ctx.FixedTypes[idx] != null)
                return ctx.FixedTypes[idx]!;
            return typeof(object);
        }
        if (type.IsGenericType)
        {
            var args = type.GetGenericArguments();
            var substituted = new Type[args.Length];
            for (var j = 0; j < args.Length; j++)
                substituted[j] = SubstituteSingle(args[j], ctx);
            try
            {
                return type.GetGenericTypeDefinition().MakeGenericType(substituted);
            }
            catch (ArgumentException)
            {
                return type;
            }
        }
        return type;
    }

    // ECMA-334 §12.6.3.9 exact inference.
    private static void ExactInference(Type u, Type v, InferenceContext ctx)
    {
        var vIdx = ctx.IndexOf(v);
        if (vIdx >= 0 && !ctx.IsFixed[vIdx])
        {
            ctx.ExactBounds[vIdx].Add(u);
            return;
        }

        var uNullable = Nullable.GetUnderlyingType(u);
        var vNullable = Nullable.GetUnderlyingType(v);
        if (uNullable != null && vNullable != null)
        {
            ExactInference(uNullable, vNullable, ctx);
            return;
        }

        if (u.IsArray && v.IsArray && u.GetArrayRank() == v.GetArrayRank())
        {
            ExactInference(u.GetElementType()!, v.GetElementType()!, ctx);
            return;
        }

        if (v.IsGenericType && u.IsGenericType && v.GetGenericTypeDefinition() == u.GetGenericTypeDefinition())
        {
            var vArgs = v.GetGenericArguments();
            var uArgs = u.GetGenericArguments();
            for (var i = 0; i < vArgs.Length && i < uArgs.Length; i++)
                ExactInference(uArgs[i], vArgs[i], ctx);
        }
    }

    // ECMA-334 §12.6.3.10 lower-bound inference.
    private static void LowerBoundInference(Type u, Type v, InferenceContext ctx)
    {
        var vIdx = ctx.IndexOf(v);
        if (vIdx >= 0 && !ctx.IsFixed[vIdx])
        {
            ctx.LowerBounds[vIdx].Add(u);
            return;
        }

        var uNullable = Nullable.GetUnderlyingType(u);
        var vNullable = Nullable.GetUnderlyingType(v);
        if (uNullable != null && vNullable != null)
        {
            LowerBoundInference(uNullable, vNullable, ctx);
            return;
        }

        if (u.IsArray && v.IsArray && u.GetArrayRank() == v.GetArrayRank())
        {
            var uElem = u.GetElementType()!;
            var vElem = v.GetElementType()!;
            if (!uElem.IsValueType && !vElem.IsValueType)
                LowerBoundInference(uElem, vElem, ctx);
            else
                ExactInference(uElem, vElem, ctx);
            return;
        }

        if (v.IsGenericType)
        {
            var vGenericDef = v.GetGenericTypeDefinition();
            var matchingU = FindUniqueConstructedType(u, vGenericDef);
            if (matchingU != null)
            {
                var vArgs = v.GetGenericArguments();
                var uArgs = matchingU.GetGenericArguments();
                var defParams = vGenericDef.GetGenericArguments();

                for (var i = 0; i < vArgs.Length && i < uArgs.Length; i++)
                {
                    var variance = defParams[i].GenericParameterAttributes & GenericParameterAttributes.VarianceMask;
                    switch (variance)
                    {
                        case GenericParameterAttributes.Covariant:
                            LowerBoundInference(uArgs[i], vArgs[i], ctx);
                            break;
                        case GenericParameterAttributes.Contravariant:
                            UpperBoundInference(uArgs[i], vArgs[i], ctx);
                            break;
                        default:
                            ExactInference(uArgs[i], vArgs[i], ctx);
                            break;
                    }
                }
            }
        }
    }

    // ECMA-334 section 12.6.3.11: Upper-bound inference
    private static void UpperBoundInference(Type u, Type v, InferenceContext ctx)
    {
        var vIdx = ctx.IndexOf(v);
        if (vIdx >= 0 && !ctx.IsFixed[vIdx])
        {
            ctx.UpperBounds[vIdx].Add(u);
            return;
        }

        var uNullable = Nullable.GetUnderlyingType(u);
        var vNullable = Nullable.GetUnderlyingType(v);
        if (uNullable != null && vNullable != null)
        {
            UpperBoundInference(uNullable, vNullable, ctx);
            return;
        }

        if (u.IsArray && v.IsArray && u.GetArrayRank() == v.GetArrayRank())
        {
            var uElem = u.GetElementType()!;
            var vElem = v.GetElementType()!;
            if (!uElem.IsValueType && !vElem.IsValueType)
                UpperBoundInference(uElem, vElem, ctx);
            else
                ExactInference(uElem, vElem, ctx);
            return;
        }

        if (u.IsGenericType)
        {
            var uGenericDef = u.GetGenericTypeDefinition();
            var matchingV = FindUniqueConstructedType(v, uGenericDef);
            if (matchingV != null)
            {
                var uArgs = u.GetGenericArguments();
                var vArgs = matchingV.GetGenericArguments();
                var defParams = uGenericDef.GetGenericArguments();

                for (var i = 0; i < uArgs.Length && i < vArgs.Length; i++)
                {
                    var variance = defParams[i].GenericParameterAttributes & GenericParameterAttributes.VarianceMask;
                    switch (variance)
                    {
                        case GenericParameterAttributes.Covariant:
                            UpperBoundInference(vArgs[i], uArgs[i], ctx);
                            break;
                        case GenericParameterAttributes.Contravariant:
                            LowerBoundInference(vArgs[i], uArgs[i], ctx);
                            break;
                        default:
                            ExactInference(vArgs[i], uArgs[i], ctx);
                            break;
                    }
                }
            }
        }
    }

    // ECMA-334 section 12.6.3.2: Explicit parameter type inference for lambdas
    private static void ExplicitParameterTypeInference(object lambdaArg, Type paramType, InferenceContext ctx)
    {
        if (!paramType.IsGenericType)
            return;

        var paramGenericDef = paramType.GetGenericTypeDefinition();
        if (!IsFuncType(paramGenericDef) && !IsActionType(paramGenericDef))
            return;

        var paramGenericArgs = paramType.GetGenericArguments();
        var lambdaParamCount = GetLambdaParameterCount(lambdaArg);
        if (lambdaParamCount < 0)
            return;

        // For Func<T1..Tn, TResult>, input params are 0..n-1
        // For Action<T1..Tn>, all params are input
        var inputCount = IsFuncType(paramGenericDef) ? paramGenericArgs.Length - 1 : paramGenericArgs.Length;
        if (lambdaParamCount != inputCount)
            return;

        // Alder lambdas don't have explicit parameter types (they're dynamically typed),
        // so explicit parameter type inference doesn't apply. This method exists for
        // completeness but no bounds are added.
    }

    // ECMA-334 section 12.6.3.12: Fixing
    private static bool TryFixParam(InferenceContext ctx, int index)
    {
        var exact = ctx.ExactBounds[index];
        var lower = ctx.LowerBounds[index];
        var upper = ctx.UpperBounds[index];

        // Candidate set = union of all bounds
        var candidates = new HashSet<Type>();
        candidates.UnionWith(exact);
        candidates.UnionWith(lower);
        candidates.UnionWith(upper);

        if (candidates.Count == 0)
            return false;

        // Remove candidates not identical to every exact bound
        if (exact.Count > 0)
            candidates.IntersectWith(exact);

        if (candidates.Count == 0)
            return false;

        // Remove candidates that don't have implicit conversion FROM every lower bound
        if (lower.Count > 0)
            candidates.RemoveWhere(c => !AllLowerBoundsConvertTo(lower, c));

        if (candidates.Count == 0)
            return false;

        // Remove candidates that don't have implicit conversion TO every upper bound
        if (upper.Count > 0)
            candidates.RemoveWhere(c => !AllUpperBoundsConvertFrom(c, upper));

        if (candidates.Count == 0)
            return false;

        // Find unique V where all other candidates convert to V
        Type? best = null;
        foreach (var candidate in candidates)
        {
            var allConvert = true;
            foreach (var other in candidates)
            {
                if (other == candidate)
                    continue;
                if (!TypeHelpers.CanImplicitlyConvert(other, candidate))
                {
                    allConvert = false;
                    break;
                }
            }
            if (allConvert)
            {
                if (best != null)
                    return false; // ambiguous
                best = candidate;
            }
        }

        if (best == null)
            return false;

        ctx.IsFixed[index] = true;
        ctx.FixedTypes[index] = best;
        return true;
    }

    private static bool AllLowerBoundsConvertTo(HashSet<Type> lowerBounds, Type target)
    {
        foreach (var lb in lowerBounds)
        {
            if (!TypeHelpers.CanImplicitlyConvert(lb, target))
                return false;
        }
        return true;
    }

    private static bool AllUpperBoundsConvertFrom(Type source, HashSet<Type> upperBounds)
    {
        foreach (var ub in upperBounds)
        {
            if (!TypeHelpers.CanImplicitlyConvert(source, ub))
                return false;
        }
        return true;
    }

    private static Type? FindUniqueConstructedType(Type type, Type genericDef)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == genericDef)
            return type;

        Type? match = null;

        foreach (var iface in ReflectionRuntime.GetInterfaces(type))
        {
            if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != genericDef)
                continue;

            if (match != null)
                return null; // not unique
            match = iface;
        }

        if (match != null)
            return match;

        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == genericDef)
                return baseType;
            baseType = baseType.BaseType;
        }

        return null;
    }

    private static bool[,] ComputeDependencies(InferenceContext ctx, ParameterInfo[] parameters, object?[]? lambdaArgs)
    {
        var n = ctx.GenericParams.Length;
        var depends = new bool[n, n];

        if (lambdaArgs == null)
            return depends;

        for (var i = 0; i < parameters.Length && i < lambdaArgs.Length; i++)
        {
            if (lambdaArgs[i] == null)
                continue;

            var paramType = parameters[i].ParameterType;
            if (!paramType.IsGenericType)
                continue;

            var paramGenericDef = paramType.GetGenericTypeDefinition();
            if (!IsFuncType(paramGenericDef))
                continue;

            var paramGenericArgs = paramType.GetGenericArguments();
            var inputTypes = new Type[paramGenericArgs.Length - 1];
            Array.Copy(paramGenericArgs, inputTypes, inputTypes.Length);
            var outputType = paramGenericArgs[^1];

            var outputIndices = CollectGenericParamIndices(outputType, ctx);
            var inputIndices = new HashSet<int>();
            foreach (var inp in inputTypes)
                inputIndices.UnionWith(CollectGenericParamIndices(inp, ctx));

            // Xi depends on Xj if Xj in inputs and Xi in outputs
            foreach (var xi in outputIndices)
            {
                foreach (var xj in inputIndices)
                {
                    if (xi != xj)
                        depends[xi, xj] = true;
                }
            }
        }

        return depends;
    }

    private static HashSet<int> CollectGenericParamIndices(Type type, InferenceContext ctx)
    {
        var indices = new HashSet<int>();
        CollectGenericParamIndicesCore(type, ctx, indices);
        return indices;
    }

    private static void CollectGenericParamIndicesCore(Type type, InferenceContext ctx, HashSet<int> indices)
    {
        if (type.IsGenericParameter)
        {
            var idx = ctx.IndexOf(type);
            if (idx >= 0)
                indices.Add(idx);
            return;
        }
        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
                CollectGenericParamIndicesCore(arg, ctx, indices);
        }
        if (type.HasElementType)
            CollectGenericParamIndicesCore(type.GetElementType()!, ctx, indices);
    }

    private static bool HasDependencyOnUnfixed(int paramIndex, bool[,] dependsOn, InferenceContext ctx)
    {
        for (var j = 0; j < ctx.GenericParams.Length; j++)
        {
            if (dependsOn[paramIndex, j] && !ctx.IsFixed[j])
                return true;
        }
        return false;
    }

    private static bool IsDependedOnByOther(int paramIndex, bool[,] dependsOn, InferenceContext ctx)
    {
        for (var j = 0; j < ctx.GenericParams.Length; j++)
        {
            if (j == paramIndex)
                continue;
            if (!ctx.IsFixed[j] && dependsOn[j, paramIndex])
                return true;
        }
        return false;
    }

    private static bool IsLambdaOutputParam(int paramIndex, InferenceContext ctx, ParameterInfo[] parameters, object?[] lambdaArgs)
    {
        var genericParam = ctx.GenericParams[paramIndex];
        for (var i = 0; i < parameters.Length && i < lambdaArgs.Length; i++)
        {
            if (lambdaArgs[i] == null)
                continue;

            var paramType = parameters[i].ParameterType;
            if (!paramType.IsGenericType)
                continue;

            var paramGenericDef = paramType.GetGenericTypeDefinition();
            if (!IsFuncType(paramGenericDef))
                continue;

            var outputType = paramType.GetGenericArguments()[^1];
            if (ContainsGenericParam(outputType, genericParam))
                return true;
        }
        return false;
    }

    private static bool ContainsGenericParam(Type type, Type genericParam)
    {
        if (type == genericParam)
            return true;
        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                if (ContainsGenericParam(arg, genericParam))
                    return true;
            }
        }
        return false;
    }

    private static bool HasAnyBounds(InferenceContext ctx, int index) =>
        ctx.ExactBounds[index].Count > 0 || ctx.LowerBounds[index].Count > 0 || ctx.UpperBounds[index].Count > 0;

    private static bool AllFixed(InferenceContext ctx)
    {
        for (var i = 0; i < ctx.IsFixed.Length; i++)
        {
            if (!ctx.IsFixed[i])
                return false;
        }
        return true;
    }

    private static int GetLambdaParameterCount(object lambdaArg) => lambdaArg switch
    {
        LambdaValue lv => lv.Parameters.Count,
        CompiledLambdaValue cv => cv.Parameters.Count,
        _ => -1
    };

    private static bool IsFuncType(Type genericDef) =>
        genericDef == typeof(Func<>) ||
        genericDef == typeof(Func<,>) ||
        genericDef == typeof(Func<,,>) ||
        genericDef == typeof(Func<,,,>) ||
        genericDef == typeof(Func<,,,,>) ||
        genericDef == typeof(Func<,,,,,>) ||
        genericDef == typeof(Func<,,,,,,>) ||
        genericDef == typeof(Func<,,,,,,,>) ||
        genericDef == typeof(Func<,,,,,,,,>);

    private static bool IsActionType(Type genericDef) =>
        genericDef == typeof(Action<>) ||
        genericDef == typeof(Action<,>) ||
        genericDef == typeof(Action<,,>) ||
        genericDef == typeof(Action<,,,>);

    private sealed class InferenceContext
    {
        public readonly Type[] GenericParams;
        public readonly bool[] IsFixed;
        public readonly Type?[] FixedTypes;
        public readonly HashSet<Type>[] ExactBounds;
        public readonly HashSet<Type>[] LowerBounds;
        public readonly HashSet<Type>[] UpperBounds;

        public InferenceContext(Type[] genericParams)
        {
            GenericParams = genericParams;
            var n = genericParams.Length;
            IsFixed = new bool[n];
            FixedTypes = new Type?[n];
            ExactBounds = new HashSet<Type>[n];
            LowerBounds = new HashSet<Type>[n];
            UpperBounds = new HashSet<Type>[n];
            for (var i = 0; i < n; i++)
            {
                ExactBounds[i] = new HashSet<Type>();
                LowerBounds[i] = new HashSet<Type>();
                UpperBounds[i] = new HashSet<Type>();
            }
        }

        public int IndexOf(Type type)
        {
            for (var i = 0; i < GenericParams.Length; i++)
            {
                if (GenericParams[i] == type)
                    return i;
            }
            return -1;
        }
    }
}
