using CsEval.Interpretation;

namespace CsEval.Runtime;

/// <summary>
/// General runtime helper methods for evaluation flow control.
/// </summary>
public static class RuntimeHelpers
{
    public static object? ResolveIdentifier(string name, CsEvalContext context, Dictionary<string, Func<object?[], object?>> functions)
    {
        if (functions.TryGetValue(name, out var function))
            return new FunctionRef(name, function);

        return context.Get(name);
    }

    public static void CheckAllowAssignment(CsEvalOptions options, string context)
    {
        if (!options.Sandbox.AllowAssignment)
            throw new CsEvalException($"Assignment blocked by sandbox: {context}");
    }

    public static void CheckAllowIndexSet(CsEvalOptions options, object? index)
    {
        if (!options.Sandbox.AllowIndexSet)
            throw new CsEvalException($"Index assignment blocked by sandbox: [{index}] = ...");
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
}
