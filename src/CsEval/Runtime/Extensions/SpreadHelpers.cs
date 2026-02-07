using CsEval.Interpretation;

namespace CsEval.Runtime.Extensions;

/// <summary>
/// Runtime helpers for spread operator and collection literal creation.
/// SpreadIntoDict/SpreadIntoList handle the ... spread syntax in object/array literals.
/// CreateTypedList infers element type and creates a typed List&lt;T&gt; from array literals.
/// Called from Evaluator (directly) and ExpressionCompilerUnit (via cached MethodInfo).
/// </summary>
public static class SpreadHelpers
{
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
}
