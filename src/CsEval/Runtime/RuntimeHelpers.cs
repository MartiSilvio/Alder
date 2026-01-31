using CsEval.Interpretation;

namespace CsEval.Runtime;

/// <summary>
/// General runtime helper methods for evaluation flow control.
/// </summary>
public static class RuntimeHelpers
{
    public static object? ResolveIdentifier(string name, CsEvalContext context, Dictionary<string, Func<object?[], object?>> functions)
    {
        if (functions.ContainsKey(name))
            return new FunctionRef(name, functions[name]);

        return context.Get(name);
    }

    public static void CheckAllowAssignment(CsEvalOptions options, string context)
    {
        if (!options.Sandbox.AllowAssignment)
            throw new CsEvalException($"Assignment blocked by sandbox: {context}");
    }

    public static void CheckIterationLimit(long iterations, CsEvalOptions options)
    {
        if (options.MaxIterations > 0 && iterations > options.MaxIterations)
            throw new CsEvalException($"Loop exceeded maximum iterations ({options.MaxIterations}). Possible infinite loop.");
    }

    public static System.Collections.IEnumerator GetEnumerator(object? collection)
    {
        if (collection is not System.Collections.IEnumerable enumerable)
            throw new CsEvalException($"Cannot iterate over type '{collection?.GetType().Name ?? "null"}' in foreach");

        return enumerable.GetEnumerator();
    }
}
