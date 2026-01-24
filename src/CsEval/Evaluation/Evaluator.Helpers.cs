using System.Reflection;

namespace CsEval.Evaluation;

public sealed partial class Evaluator
{
    /// <summary>
    /// Checks if a type is a forbidden reflection metadata type.
    /// User code must never obtain a value whose runtime type is System.Type or any reflection metadata type.
    /// </summary>
    private static bool IsForbiddenReflectionType(Type? type)
    {
        if (type == null) return false;

        // Block System.Type (includes RuntimeType)
        if (typeof(Type).IsAssignableFrom(type))
            return true;

        // Block all reflection metadata types (MemberInfo is base for MethodInfo, PropertyInfo, FieldInfo, etc.)
        if (typeof(MemberInfo).IsAssignableFrom(type))
            return true;

        // Block Assembly and Module
        if (typeof(Assembly).IsAssignableFrom(type))
            return true;
        if (typeof(Module).IsAssignableFrom(type))
            return true;

        // Block runtime handles
        if (type == typeof(RuntimeTypeHandle) ||
            type == typeof(RuntimeMethodHandle) ||
            type == typeof(RuntimeFieldHandle))
            return true;

        return false;
    }

    /// <summary>
    /// Guards against reflection type leaks. Throws if value is a forbidden reflection type.
    /// </summary>
    private static object? GuardReflectionLeak(object? value, string context)
    {
        if (value == null) return null;

        var type = value.GetType();
        if (IsForbiddenReflectionType(type))
        {
            throw new EvalException($"Access to reflection types is not allowed: {type.Name} ({context})");
        }

        return value;
    }

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
        var methodName = methodRef.Method.Name;
        var resolver = methodRef.Resolver;
        var target = methodRef.Method.IsStatic ? null : resolver.Resolve();

        // Get all overloads of this method for proper resolution
        var methods = TypeCache.GetMethods(resolver.Type, methodName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();

            // Skip generic methods that can't be invoked directly
            if (method.ContainsGenericParameters)
            {
                // Try to infer generic type arguments from the actual arguments
                var concreteMethod = TryMakeConcreteMethod(method, args);
                if (concreteMethod != null)
                {
                    var result = InvokeMethodWithArgs(concreteMethod, target, args);
                    if (result.Success)
                        return result.Value;
                }
                continue;
            }

            var invokeResult = InvokeMethodWithArgs(method, target, args);
            if (invokeResult.Success)
                return invokeResult.Value;
        }

        // Fallback to original method if no overload matched
        var fallbackMethod = methodRef.Method;
        var fallbackParams = fallbackMethod.GetParameters();
        var finalArgs = TryAppendCancellationToken(fallbackParams, args);

        if (_argumentTransformer != null)
            finalArgs = _argumentTransformer(fallbackMethod, finalArgs);

        finalArgs = PadWithDefaults(fallbackParams, finalArgs);

        var fallbackResult = fallbackMethod.Invoke(target, finalArgs);
        return GuardReflectionLeak(UnwrapTask(fallbackResult), $"method {methodName}");
    }

    private (bool Success, object? Value) InvokeMethodWithArgs(MethodInfo method, object? target, object?[] args)
    {
        var parameters = method.GetParameters();
        var argsWithCancellation = TryAppendCancellationToken(parameters, args);

        if (CanInvokeMethod(parameters, argsWithCancellation, out var convertedArgs))
        {
            if (_argumentTransformer != null)
                convertedArgs = _argumentTransformer(method, convertedArgs);

            var result = method.Invoke(target, convertedArgs);
            return (true, GuardReflectionLeak(UnwrapTask(result), $"method {method.Name}"));
        }

        return (false, null);
    }

    private static MethodInfo? TryMakeConcreteMethod(MethodInfo genericMethod, object?[] args)
    {
        var genericArgs = genericMethod.GetGenericArguments();
        var parameters = genericMethod.GetParameters();

        if (genericArgs.Length != 1 || args.Length == 0)
            return null;

        // Try to infer the type from the first argument
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

    private static object?[] PadWithDefaults(ParameterInfo[] parameters, object?[] args)
    {
        if (parameters.Length == 0)
            return [];

        // Check if last parameter is params array
        var lastParam = parameters[^1];
        var isParams = lastParam.IsDefined(typeof(ParamArrayAttribute), false);

        if (isParams)
        {
            return PadWithParamsArray(parameters, args, lastParam);
        }

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

    private static object?[] PadWithParamsArray(ParameterInfo[] parameters, object?[] args, ParameterInfo paramsParam)
    {
        var normalParamCount = parameters.Length - 1;
        var result = new object?[parameters.Length];

        // Fill normal parameters
        for (var i = 0; i < normalParamCount; i++)
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

        // Collect remaining args into params array
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

    private object? GetMember(object obj, string name)
    {
        if (obj is CsEvalEngine.ModuleResolver resolver)
        {
            if (resolver.Members.TryGetValue(name, out var member))
            {
                return member switch
                {
                    MethodInfo m => new ModuleMethodRef(resolver, m),
                    PropertyInfo p => GuardReflectionLeak(TypeCache.GetPropertyValue(p, resolver.Resolve()!), $"property {name}"),
                    _ => throw new EvalException($"Unsupported member type '{member.GetType().Name}'")
                };
            }
            throw new EvalException($"Member '{name}' not found on module '{resolver.Type.Name}'");
        }

        if (!_options.Sandbox.AllowPropertyRead)
            throw new EvalException($"Property access blocked by sandbox: {name}");

        var ignoreCase = _options.IgnoreCase;

        if (obj is IDictionary<string, object?> dict)
        {
            if (dict.TryGetValue(name, out var value))
                return GuardReflectionLeak(value, $"property {name}");

            if (ignoreCase)
            {
                foreach (var key in dict.Keys)
                {
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        return GuardReflectionLeak(dict[key], $"property {name}");
                }
            }

            throw new EvalException($"Property '{name}' not found");
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (ignoreCase)
            bindingFlags |= BindingFlags.IgnoreCase;

        var prop = TypeCache.GetProperty(type, name, bindingFlags);
        if (prop != null)
            return GuardReflectionLeak(TypeCache.GetPropertyValue(prop, obj), $"property {name}");

        var field = TypeCache.GetField(type, name, bindingFlags);
        if (field != null)
            return GuardReflectionLeak(field.GetValue(obj), $"field {name}");

        throw new EvalException($"Property '{name}' not found on type '{type.Name}'");
    }

    private object? GetIndex(object obj, object? index)
    {
        if (obj is IDictionary<string, object?> dict && index is string strKey)
        {
            if (dict.TryGetValue(strKey, out var value))
                return GuardReflectionLeak(value, $"index [{strKey}]");
            return null;
        }

        if (obj is IList list && index != null)
        {
            var idx = Convert.ToInt32(index);
            if (idx < 0 || idx >= list.Count)
                throw new EvalException($"Index {idx} out of range");
            return GuardReflectionLeak(list[idx], $"index [{idx}]");
        }

        var type = obj.GetType();
        var indexer = TypeCache.GetIndexer(type);
        if (indexer != null)
            return GuardReflectionLeak(indexer.GetValue(obj, [index]), $"indexer access");

        throw new EvalException($"Cannot index type '{type.Name}'");
    }

    private void SetIndex(object obj, object? index, object? value)
    {
        if (!_options.Sandbox.AllowIndexSet)
            throw new EvalException($"Index assignment blocked by sandbox: [{index}] = ...");

        if (obj is IDictionary<string, object?> dict && index is string strKey)
        {
            dict[strKey] = value;
            return;
        }

        if (obj is IList list && index != null)
        {
            var idx = Convert.ToInt32(index);
            if (idx < 0 || idx >= list.Count)
                throw new EvalException($"Index {idx} out of range");
            list[idx] = value;
            return;
        }

        var type = obj.GetType();
        var indexer = TypeCache.GetIndexer(type);
        if (indexer != null && indexer.CanWrite)
        {
            indexer.SetValue(obj, value, [index]);
            return;
        }

        throw new EvalException($"Cannot set index on type '{type.Name}'");
    }

    private void SetMember(object obj, string name, object? value)
    {
        if (!_options.Sandbox.AllowPropertySet)
            throw new EvalException($"Property assignment blocked by sandbox: {name} = ...");

        var ignoreCase = _options.IgnoreCase;

        if (obj is IDictionary<string, object?> dict)
        {
            // For dictionaries, try to find existing key with case-insensitive match
            if (ignoreCase)
            {
                foreach (var key in dict.Keys)
                {
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        dict[key] = value;
                        return;
                    }
                }
            }
            // If no match found or case-sensitive, use the provided name
            dict[name] = value;
            return;
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (ignoreCase)
            bindingFlags |= BindingFlags.IgnoreCase;

        var prop = TypeCache.GetProperty(type, name, bindingFlags);
        if (prop != null)
        {
            if (!prop.CanWrite)
                throw new EvalException($"Property '{name}' is read-only");
            prop.SetValue(obj, value);
            return;
        }

        var field = TypeCache.GetField(type, name, bindingFlags);
        if (field != null)
        {
            if (field.IsInitOnly)
                throw new EvalException($"Field '{name}' is read-only");
            field.SetValue(obj, value);
            return;
        }

        throw new EvalException($"Property '{name}' not found on type '{type.Name}'");
    }

    private (bool Success, object? Value) TryInvokeMethod(object target, string methodName, object?[] args)
    {
        if (target is CsEvalEngine.ModuleResolver)
            return (false, null);

        var type = target.GetType();

        // LINQ methods are always allowed (handled internally, not via reflection)
        if (target is IEnumerable enumerable && !target.GetType().IsPrimitive && target is not string)
        {
            var result = TryInvokeEnumerableMethod(enumerable, methodName, args);
            if (result.Success)
                return result;
        }

        // Sandbox blocks method calls on variable objects when AllowMethodCalls is false
        if (_options.Sandbox.BlockMethodCalls)
            throw new EvalException($"Method calls blocked by sandbox: {methodName}");

        var methods = TypeCache.GetMethods(type, methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var argsWithCancellation = TryAppendCancellationToken(parameters, args);
            if (CanInvokeMethod(parameters, argsWithCancellation, out var convertedArgs))
            {
                var result = method.Invoke(target, convertedArgs);
                return (true, GuardReflectionLeak(UnwrapTask(result), $"method {methodName}"));
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

            if (task.GetType().IsGenericType)
                return ((dynamic)task).Result;

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
