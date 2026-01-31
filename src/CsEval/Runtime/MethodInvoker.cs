using CsEval.Interpretation;

namespace CsEval.Runtime;

/// <summary>
/// Method invocation and dispatch.
/// </summary>
public static class MethodInvoker
{
    public static object? InvokeMemberCall(
        object? target,
        string methodName,
        object?[] args,
        bool nullSafe,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        Dictionary<string, Func<object?[], object?>> functions,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer)
    {
        if (nullSafe && target == null)
            return null;

        if (target == null)
            throw new CsEvalException($"Cannot call method '{methodName}' on null");

        var result = TryInvokeInstanceMethod(target, methodName, args, context, options, ct, argumentTransformer);
        if (result.Success)
            return result.Value;

        var callee = MemberAccess.GetMember(target, methodName, options, nullSafe, context);
        return InvokeCall(callee, args, functions, context, options, ct, argumentTransformer);
    }

    public static object? InvokeCall(
        object? callee,
        object?[] args,
        Dictionary<string, Func<object?[], object?>> functions,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer)
    {
        if (callee is MethodRef methodRef)
        {
            var result = TryInvokeInstanceMethod(methodRef.Target, methodRef.MethodName, args, context, options, ct, argumentTransformer);
            if (result.Success)
                return result.Value;
            throw new CsEvalException($"Method '{methodRef.MethodName}' invocation failed");
        }

        if (callee is ModuleMethodRef moduleRef)
        {
            return InvokeModuleMethod(moduleRef, args, context, ct, argumentTransformer);
        }

        if (callee is FunctionRef funcRef)
            return funcRef.Invoke(args);

        if (callee is Delegate del)
            return del.DynamicInvoke(args);

        if (callee is LambdaValue lambda)
            return InvokeLambda(lambda, args, context);

        throw new CsEvalException($"Cannot call '{callee?.GetType().Name ?? "null"}' as a function");
    }

    public static (bool Success, object? Value) TryInvokeInstanceMethod(
        object? target,
        string methodName,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer)
    {
        if (target == null)
            return (false, null);

        if (target is CsEvalEngine.ModuleResolver)
            return (false, null);

        var type = target.GetType();

        if (target is IEnumerable enumerable && !type.IsPrimitive && target is not string)
        {
            var result = LinqDispatcher.TryInvokeEnumerableMethod(enumerable, methodName, args, context, options, ct);
            if (result.Success)
                return result;
        }

        if (options.Sandbox.BlockMethodCalls)
            throw new CsEvalException($"Method calls blocked by sandbox: {methodName}");

        var methods = context.TypeCache.GetMethods(type, methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var argsWithCancellation = TryAppendCancellationToken(parameters, args, ct);
            if (CanInvokeMethod(parameters, argsWithCancellation, out var convertedArgs))
            {
                if (argumentTransformer != null)
                    convertedArgs = argumentTransformer(method, convertedArgs);

                var result = method.Invoke(target, convertedArgs);
                return (true, GuardReflectionLeak(result, $"method {methodName}"));
            }
        }

        return (false, null);
    }

    private static object? InvokeModuleMethod(
        ModuleMethodRef methodRef,
        object?[] args,
        CsEvalContext context,
        CancellationToken ct,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer)
    {
        var methodName = methodRef.Method.Name;
        var resolver = methodRef.Resolver;
        var target = methodRef.Method.IsStatic ? null : resolver.Resolve();

        var methods = context.TypeCache.GetMethods(resolver.Type, methodName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        foreach (var method in methods)
        {
            if (method.ContainsGenericParameters)
            {
                var concreteMethod = TryMakeConcreteMethod(method, args);
                if (concreteMethod != null)
                {
                    var result = InvokeMethodWithArgs(concreteMethod, target, args, ct, argumentTransformer);
                    if (result.Success)
                        return result.Value;
                }
                continue;
            }

            var invokeResult = InvokeMethodWithArgs(method, target, args, ct, argumentTransformer);
            if (invokeResult.Success)
                return invokeResult.Value;
        }

        var fallbackMethod = methodRef.Method;
        var fallbackParams = fallbackMethod.GetParameters();
        var finalArgs = TryAppendCancellationToken(fallbackParams, args, ct);

        if (argumentTransformer != null)
            finalArgs = argumentTransformer(fallbackMethod, finalArgs);

        finalArgs = PadWithDefaults(fallbackParams, finalArgs);

        var fallbackResult = fallbackMethod.Invoke(target, finalArgs);
        return GuardReflectionLeak(fallbackResult, $"method {methodName}");
    }

    private static (bool Success, object? Value) InvokeMethodWithArgs(
        MethodInfo method,
        object? target,
        object?[] args,
        CancellationToken ct,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer)
    {
        var parameters = method.GetParameters();
        var argsWithCancellation = TryAppendCancellationToken(parameters, args, ct);

        if (CanInvokeMethod(parameters, argsWithCancellation, out var convertedArgs))
        {
            if (argumentTransformer != null)
                convertedArgs = argumentTransformer(method, convertedArgs);

            var result = method.Invoke(target, convertedArgs);
            return (true, GuardReflectionLeak(result, $"method {method.Name}"));
        }

        return (false, null);
    }

    private static MethodInfo? TryMakeConcreteMethod(MethodInfo genericMethod, object?[] args)
    {
        var genericArgs = genericMethod.GetGenericArguments();

        if (genericArgs.Length != 1 || args.Length == 0)
            return null;

        var firstArg = args[0];
        if (firstArg == null)
            return null;

        try
        {
            return genericMethod.MakeGenericMethod(firstArg.GetType());
        }
        catch
        {
            return null;
        }
    }

    private static object?[] TryAppendCancellationToken(ParameterInfo[] parameters, object?[] args, CancellationToken ct)
    {
        if (parameters.Length == 0)
            return args;

        var lastParam = parameters[^1];
        if (lastParam.ParameterType == typeof(CancellationToken) && args.Length == parameters.Length - 1)
        {
            var newArgs = new object?[args.Length + 1];
            Array.Copy(args, newArgs, args.Length);
            newArgs[^1] = ct;
            return newArgs;
        }

        return args;
    }

    private static bool CanInvokeMethod(ParameterInfo[] parameters, object?[] args, out object?[] convertedArgs)
    {
        convertedArgs = new object?[parameters.Length];

        var positionalArgs = new List<object?>();
        var namedArgs = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var arg in args)
        {
            if (arg is NamedArg named)
                namedArgs[named.Name] = named.Value;
            else
                positionalArgs.Add(arg);
        }

        var filledParams = new bool[parameters.Length];
        var positionalIndex = 0;

        for (var i = 0; i < parameters.Length && positionalIndex < positionalArgs.Count; i++)
        {
            if (namedArgs.ContainsKey(parameters[i].Name!))
                continue;

            var arg = positionalArgs[positionalIndex++];
            if (!TryConvertArg(arg, parameters[i].ParameterType, out var converted))
                return false;

            convertedArgs[i] = converted;
            filledParams[i] = true;
        }

        if (positionalIndex < positionalArgs.Count)
            return false;

        foreach (var (name, value) in namedArgs)
        {
            var paramIndex = -1;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (string.Equals(parameters[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    paramIndex = i;
                    break;
                }
            }

            if (paramIndex == -1)
                return false;

            if (!TryConvertArg(value, parameters[paramIndex].ParameterType, out var converted))
                return false;

            convertedArgs[paramIndex] = converted;
            filledParams[paramIndex] = true;
        }

        for (var i = 0; i < parameters.Length; i++)
        {
            if (filledParams[i])
                continue;

            if (parameters[i].HasDefaultValue)
                convertedArgs[i] = parameters[i].DefaultValue;
            else
                return false;
        }

        return true;
    }

    private static bool TryConvertArg(object? arg, Type targetType, out object? converted)
    {
        converted = null;

        if (arg == null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                return false;
            return true;
        }

        if (targetType.IsAssignableFrom(arg.GetType()))
        {
            converted = arg;
            return true;
        }

        try
        {
            converted = Convert.ChangeType(arg, targetType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static object?[] PadWithDefaults(ParameterInfo[] parameters, object?[] args)
    {
        if (parameters.Length == 0)
            return [];

        var lastParam = parameters[^1];
        var isParams = lastParam.IsDefined(typeof(ParamArrayAttribute), false);

        if (isParams)
            return PadWithParamsArray(parameters, args, lastParam);

        var result = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            if (i < args.Length)
                result[i] = CoerceNumeric(args[i], parameters[i].ParameterType);
            else if (parameters[i].HasDefaultValue)
                result[i] = parameters[i].DefaultValue;
            else
                throw new CsEvalException($"Missing required argument '{parameters[i].Name}'");
        }

        return result;
    }

    private static object?[] PadWithParamsArray(ParameterInfo[] parameters, object?[] args, ParameterInfo paramsParam)
    {
        var normalParamCount = parameters.Length - 1;
        var result = new object?[parameters.Length];

        for (var i = 0; i < normalParamCount; i++)
        {
            if (i < args.Length)
                result[i] = CoerceNumeric(args[i], parameters[i].ParameterType);
            else if (parameters[i].HasDefaultValue)
                result[i] = parameters[i].DefaultValue;
            else
                throw new CsEvalException($"Missing required argument '{parameters[i].Name}'");
        }

        var paramsElementType = paramsParam.ParameterType.GetElementType()!;
        var paramsCount = Math.Max(0, args.Length - normalParamCount);
        var paramsArray = Array.CreateInstance(paramsElementType, paramsCount);

        for (var i = 0; i < paramsCount; i++)
        {
            var value = CoerceNumeric(args[normalParamCount + i], paramsElementType);
            paramsArray.SetValue(value, i);
        }

        result[normalParamCount] = paramsArray;
        return result;
    }

    private static object? CoerceNumeric(object? arg, Type targetType)
    {
        if (arg == null) return null;
        if (targetType.IsInstanceOfType(arg)) return arg;

        if (arg is IConvertible)
        {
            try
            {
                var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
                return Convert.ChangeType(arg, underlying);
            }
            catch
            {
                return arg;
            }
        }

        return arg;
    }

    private static object? GuardReflectionLeak(object? value, string context)
    {
        if (value == null) return null;

        var type = value.GetType();
        if (TypeHelpers.IsForbiddenReflectionType(type))
            throw new CsEvalException($"Access to reflection types is not allowed: {type.Name} ({context})");

        return value;
    }

    internal static object? InvokeLambda(LambdaValue lambda, object?[] args, CsEvalContext context)
    {
        var childContext = lambda.Closure.CreateChild();
        for (var i = 0; i < lambda.Parameters.Count && i < args.Length; i++)
        {
            childContext.Define(lambda.Parameters[i], args[i]);
        }

        var evaluator = new Evaluator(childContext, new Dictionary<string, Func<object?[], object?>>());
        return evaluator.Evaluate(lambda.Body);
    }
}
