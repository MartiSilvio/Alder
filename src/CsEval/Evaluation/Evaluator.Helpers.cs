namespace CsEval.Evaluation;

public sealed partial class Evaluator
{
    private object? InvokeLambda(LambdaValue lambda, object?[] args)
    {
        var childContext = lambda.Closure.CreateChild();
        for (var i = 0; i < lambda.Parameters.Count && i < args.Length; i++)
        {
            childContext.Define(lambda.Parameters[i], args[i]);
        }

        var previousContext = _context;
        _context = childContext;
        try
        {
            return Evaluate(lambda.Body);
        }
        finally
        {
            _context = previousContext;
        }
    }

    private object? InvokeModuleMethod(ModuleMethodRef methodRef, object?[] args)
    {
        var method = methodRef.Method;
        var target = method.IsStatic ? null : methodRef.Resolver.Resolve();
        var parameters = method.GetParameters();

        var finalArgs = TryAppendCancellationToken(parameters, args);

        if (_argumentTransformer != null)
            finalArgs = _argumentTransformer(method, finalArgs);

        finalArgs = PadWithDefaults(parameters, finalArgs);

        var result = method.Invoke(target, finalArgs);
        return UnwrapTask(result);
    }

    private static object?[] PadWithDefaults(ParameterInfo[] parameters, object?[] args)
    {
        var result = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            if (i < args.Length)
            {
                result[i] = CoerceNumeric(args[i], parameters[i].ParameterType);
            }
            else if (parameters[i].HasDefaultValue)
            {
                result[i] = parameters[i].DefaultValue;
            }
            else
            {
                throw new EvalException($"Missing required argument '{parameters[i].Name}'");
            }
        }

        return result;
    }

    private static object? CoerceNumeric(object? arg, Type targetType)
    {
        if (arg == null) return null;
        if (targetType.IsInstanceOfType(arg)) return arg;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (arg is IConvertible && IsNumericType(underlying))
            return Convert.ChangeType(arg, underlying);

        return arg;
    }

    private static bool IsNumericType(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(double) ||
        type == typeof(float) || type == typeof(decimal) || type == typeof(short) ||
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(ushort) ||
        type == typeof(uint) || type == typeof(ulong);

    private object? GetMember(object obj, string name)
    {
        if (obj is CsEvalEngine.ModuleResolver resolver)
        {
            if (resolver.Members.TryGetValue(name, out var member))
            {
                return member switch
                {
                    MethodInfo m => new ModuleMethodRef(resolver, m),
                    PropertyInfo p => p.GetValue(resolver.Resolve()),
                    _ => throw new EvalException($"Unsupported member type '{member.GetType().Name}'")
                };
            }
            throw new EvalException($"Member '{name}' not found on module '{resolver.Type.Name}'");
        }

        var ignoreCase = _options.IgnoreCase;

        if (obj is IDictionary<string, object?> dict)
        {
            if (dict.TryGetValue(name, out var value))
                return value;

            if (ignoreCase)
            {
                foreach (var key in dict.Keys)
                {
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        return dict[key];
                }
            }

            throw new EvalException($"Property '{name}' not found");
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (ignoreCase)
            bindingFlags |= BindingFlags.IgnoreCase;

        var prop = type.GetProperty(name, bindingFlags);
        if (prop != null)
            return prop.GetValue(obj);

        var field = type.GetField(name, bindingFlags);
        if (field != null)
            return field.GetValue(obj);

        throw new EvalException($"Property '{name}' not found on type '{type.Name}'");
    }

    private static object? GetIndex(object obj, object? index)
    {
        if (obj is IDictionary<string, object?> dict && index is string strKey)
        {
            if (dict.TryGetValue(strKey, out var value))
                return value;
            return null;
        }

        if (obj is IList list && index != null)
        {
            var idx = Convert.ToInt32(index);
            if (idx < 0 || idx >= list.Count)
                throw new EvalException($"Index {idx} out of range");
            return list[idx];
        }

        var type = obj.GetType();
        var indexer = type.GetProperty("Item");
        if (indexer != null)
            return indexer.GetValue(obj, [index]);

        throw new EvalException($"Cannot index type '{type.Name}'");
    }

    private (bool Success, object? Value) TryInvokeMethod(object target, string methodName, object?[] args)
    {
        if (target is CsEvalEngine.ModuleResolver)
            return (false, null);

        var type = target.GetType();

        if (target is IEnumerable enumerable && !target.GetType().IsPrimitive && target is not string)
        {
            var result = TryInvokeEnumerableMethod(enumerable, methodName, args);
            if (result.Success)
                return result;
        }

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            .Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var argsWithCancellation = TryAppendCancellationToken(parameters, args);
            if (CanInvokeMethod(parameters, argsWithCancellation, out var convertedArgs))
            {
                var result = method.Invoke(target, convertedArgs);
                return (true, UnwrapTask(result));
            }
        }

        return (false, null);
    }

    private object?[] TryAppendCancellationToken(ParameterInfo[] parameters, object?[] args)
    {
        if (parameters.Length == 0)
            return args;

        var lastParam = parameters[^1];
        if (lastParam.ParameterType == typeof(CancellationToken) && args.Length == parameters.Length - 1)
        {
            var newArgs = new object?[args.Length + 1];
            Array.Copy(args, newArgs, args.Length);
            newArgs[^1] = _cancellationToken;
            return newArgs;
        }

        return args;
    }

    private object? UnwrapTask(object? result)
    {
        if (result is Task task)
        {
            task.ConfigureAwait(false).GetAwaiter().GetResult();

            var taskType = task.GetType();
            if (taskType.IsGenericType)
            {
                var resultProperty = taskType.GetProperty("Result");
                return resultProperty?.GetValue(task);
            }

            return null;
        }

        return result;
    }

    private static bool CanInvokeMethod(ParameterInfo[] parameters, object?[] args, out object?[] convertedArgs)
    {
        convertedArgs = new object?[parameters.Length];

        if (args.Length > parameters.Length)
            return false;

        for (var i = 0; i < parameters.Length; i++)
        {
            if (i < args.Length)
            {
                if (args[i] == null)
                {
                    if (parameters[i].ParameterType.IsValueType && Nullable.GetUnderlyingType(parameters[i].ParameterType) == null)
                        return false;
                    convertedArgs[i] = null;
                }
                else if (parameters[i].ParameterType.IsAssignableFrom(args[i]!.GetType()))
                {
                    convertedArgs[i] = args[i];
                }
                else
                {
                    try
                    {
                        convertedArgs[i] = Convert.ChangeType(args[i], parameters[i].ParameterType);
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            else if (parameters[i].HasDefaultValue)
            {
                convertedArgs[i] = parameters[i].DefaultValue;
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}
