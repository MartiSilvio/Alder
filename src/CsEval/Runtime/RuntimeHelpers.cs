using CsEval.Interpretation;

namespace CsEval.Runtime;

public static class RuntimeHelpers
{
    public static object? ResolveIdentifier(string name, CsEvalContext context)
    {
        if (context.Functions.TryGetValue(name, out var function))
            return new FunctionRef(name, function);

        if (context.Modules.TryGetValue(name, out var module))
            return module;

        return context.Get(name);
    }

    public static void CheckAllowAssignment(CsEvalOptions options, string context)
    {
        if (!options.Sandbox.AllowAssignment)
            throw new CsEvalException($"Assignment blocked by sandbox: {context}");
    }

    public static void CheckNullCoalesceAssignAllowed(string name, CsEvalContext context)
    {
        if (context.TryGetVariableType(name, out var varType) && varType != null && !TypeHelpers.IsNullableType(varType))
            throw new CsEvalException($"Operator '??=' cannot be applied to operand of type '{varType.Name}'");
    }

    public static void CheckAllowIndexSet(CsEvalOptions options, object? index)
    {
        if (!options.Sandbox.AllowIndexSet)
            throw new CsEvalException($"Index assignment blocked by sandbox: [{index}] = ...");
    }

    public static object? ValidateCompoundAssignment(string name, object? result, object? rightValue, CsEvalContext context)
    {
        if (!context.TryGetVariableType(name, out var varType) || varType == null || result == null)
            return result;

        var resultType = result.GetType();
        var rightType = rightValue?.GetType();

        var resultConvertible = resultType == varType || TypeHelpers.CanImplicitlyConvert(resultType, varType);
        var rhsConvertible = rightType == null || rightType == varType ||
                             TypeHelpers.CanImplicitlyConvert(rightType, varType);

        if (!resultConvertible && !rhsConvertible)
        {
            throw new CsEvalException($"Cannot implicitly convert type '{resultType.Name}' to '{varType.Name}'");
        }

        return Convert.ChangeType(result, varType);
    }

    public static void CheckIterationLimit(long iterations, CsEvalOptions options)
    {
        if (options.MaxIterations > 0 && iterations > options.MaxIterations)
            throw new CsEvalException($"Loop exceeded maximum iterations ({options.MaxIterations}). Possible infinite loop.");
    }

    public static IEnumerator GetEnumerator(object? collection)
    {
        if (collection is not IEnumerable enumerable)
            throw new CsEvalException($"Cannot iterate over type '{collection?.GetType().Name ?? "null"}' in foreach");

        return enumerable.GetEnumerator();
    }

    public static void SpreadIntoDict(IDictionary<string, object?> target, object? source, CsEvalContext context)
    {
        switch (source)
        {
            case null:
                return;
            case IDictionary<string, object?> dict:
            {
                foreach (var kvp in dict)
                    target[kvp.Key] = kvp.Value;
                return;
            }
        }

        var type = source.GetType();
        foreach (var prop in context.TypeCache.GetProperties(type, BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.CanRead)
                target[prop.Name] = context.TypeCache.GetPropertyValue(prop, source);
        }
    }

    public static void SpreadIntoList(List<object?> target, object? source)
    {
        if (source is IEnumerable enumerable and not string)
        {
            target.AddRange(enumerable.Cast<object?>());
        }
        else
        {
            throw new CsEvalException("Spread operator requires an iterable");
        }
    }

    public static object CreateTypedList(List<object?> source)
    {
        if (source.Count == 0)
            return source;

        Type? commonType = null;
        var hasNull = false;

        foreach (var item in source)
        {
            if (item == null)
            {
                hasNull = true;
                continue;
            }

            var itemType = item.GetType();
            if (commonType == null)
                commonType = itemType;
            else if (commonType != itemType)
                return source;
        }

        if (commonType == null)
            return source;

        if (hasNull && commonType.IsValueType)
            commonType = typeof(Nullable<>).MakeGenericType(commonType);

        var listType = typeof(List<>).MakeGenericType(commonType);
        var typedList = (System.Collections.IList)Activator.CreateInstance(listType, source.Count)!;

        foreach (var item in source)
            typedList.Add(item);

        return typedList;
    }

    public static object? GetLambdaArg(object?[] args, int index)
    {
        return index < args.Length ? args[index] : null;
    }
}
