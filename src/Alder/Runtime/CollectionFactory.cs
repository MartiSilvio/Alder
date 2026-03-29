using Alder.Diagnostics;

namespace Alder.Runtime;

internal static class CollectionFactory
{
    internal static object Create(Type targetType, Type elementType, List<object?> values)
    {
        if (targetType.IsArray)
            return RuntimeArrayFactory.CreateFromValues(elementType, values);

        if (targetType.IsInterface)
            return CreateForInterface(targetType, elementType, values);

        return CreateConcrete(targetType, elementType, values);
    }

    public static void SpreadIntoList(List<object?> target, object? source)
    {
        if (source is IEnumerable enumerable and not string)
        {
            target.AddRange(enumerable.Cast<object?>());
        }
        else
        {
            throw new AlderException(DiagnosticDescriptors.ForeachRequiresIEnumerable, TypeNameFormatter.Of(source));
        }
    }

    public static void SpreadIntoDict(IDictionary<string, object?> target, object? source, AlderContext context)
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

    private static object CreateForInterface(Type interfaceType, Type elementType, List<object?> values)
    {
        var genericDef = interfaceType.IsGenericType ? interfaceType.GetGenericTypeDefinition() : null;
        if (genericDef == typeof(IEnumerable<>) ||
            genericDef == typeof(IList<>) ||
            genericDef == typeof(ICollection<>) ||
            genericDef == typeof(IReadOnlyList<>) ||
            genericDef == typeof(IReadOnlyCollection<>))
        {
            return CreateConcrete(typeof(List<>).MakeGenericType(elementType), elementType, values);
        }

        return RuntimeArrayFactory.CreateFromValues(elementType, values);
    }

    private static object CreateConcrete(Type concreteType, Type elementType, List<object?> values)
    {
        return CreateCoreGenericMethod.MakeGenericMethod(concreteType, elementType).Invoke(null, [values])!;
    }

    private static object CreateCoreTyped<TCollection, TElement>(List<object?> values)
        where TCollection : ICollection<TElement>, new()
    {
        var collection = new TCollection();
        var convertTarget = Nullable.GetUnderlyingType(typeof(TElement)) ?? typeof(TElement);
        foreach (var value in values)
        {
            if (value == null)
            {
                collection.Add(default!);
            }
            else if (value.GetType() == convertTarget || value.GetType() == typeof(TElement))
            {
                collection.Add((TElement)value);
            }
            else
            {
                collection.Add((TElement)Convert.ChangeType(value, convertTarget));
            }
        }
        return collection;
    }

    private static readonly System.Reflection.MethodInfo CreateCoreGenericMethod =
        typeof(CollectionFactory).GetMethod(nameof(CreateCoreTyped),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
}
