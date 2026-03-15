using CsEval.Diagnostics;

namespace CsEval.Runtime;

internal static class SpreadHelpers
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
        foreach (var prop in context.TypeMetadata.GetProperties(type, BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.CanRead)
                target[prop.Name] = context.TypeMetadata.GetPropertyValue(prop, source);
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
            throw new CsEvalException(DiagnosticDescriptors.ForeachRequiresIEnumerable, TypeNameFormatter.Of(source));
        }
    }

    public static object CreateTypedArray(List<object?> source)
    {
        if (source.Count == 0)
            return Array.Empty<object?>();

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
                return source.ToArray(); // mixed types -> object?[]
        }

        if (commonType == null)
            return source.ToArray(); // all nulls -> object?[]

        if (hasNull && commonType.IsValueType)
            commonType = RuntimeGenericFactory.CloseGenericType(typeof(Nullable<>), [commonType]);

        var array = RuntimeArrayFactory.Create(commonType, source.Count);
        for (var i = 0; i < source.Count; i++)
            array.SetValue(source[i], i);
        return array;
    }
}
