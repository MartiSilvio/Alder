using Alder.Diagnostics;

namespace Alder.Runtime;

internal static class CollectionFactory
{
    internal static object Create(Type targetType, Type elementType, List<object?> values, AlderConfig? config = null)
    {
        if (targetType.IsArray)
            return RuntimeArrayFactory.CreateFromValues(elementType, values);

        if (targetType.IsInterface)
            return CreateForInterface(targetType, elementType, values, config);

        return CreateConcrete(targetType, elementType, values, config);
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

    private static object CreateForInterface(Type interfaceType, Type elementType, List<object?> values, AlderConfig? config)
    {
        // For interfaces (IList<T>, IEnumerable<T>, etc.), the concrete type is List<T>.
        // Try typed dispatch first — if List<T> is registered, create via TryCreate + TryInvoke("Add").
        if (config != null)
        {
            var genericDef = interfaceType.IsGenericType ? interfaceType.GetGenericTypeDefinition() : null;
            if (genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(IList<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IReadOnlyList<>) ||
                genericDef == typeof(IReadOnlyCollection<>))
            {
                // Find the registered List<T> that matches this element type
                var listType = typeof(List<>).IsGenericTypeDefinition && MethodDispatchCache.DynamicCodeSupported
                    ? typeof(List<>).MakeGenericType(elementType)
                    : null;

                // Try each registered type to find the matching List<T>
                if (listType != null && config.TryGetDispatch(listType, out var listDispatch))
                    return CreateViaDispatch(listDispatch, elementType, values);

                // AOT fallback: try to find a registered collection type for this element type
                if (!MethodDispatchCache.DynamicCodeSupported)
                    return TryCreateViaRegisteredCollections(config, elementType, values)
                        ?? RuntimeArrayFactory.CreateFromValues(elementType, values);
            }
        }

        // JIT fallback
        var concreteGenericDef = interfaceType.IsGenericType ? interfaceType.GetGenericTypeDefinition() : null;
        if (concreteGenericDef == typeof(IEnumerable<>) ||
            concreteGenericDef == typeof(IList<>) ||
            concreteGenericDef == typeof(ICollection<>) ||
            concreteGenericDef == typeof(IReadOnlyList<>) ||
            concreteGenericDef == typeof(IReadOnlyCollection<>))
        {
            return CreateConcrete(typeof(List<>).MakeGenericType(elementType), elementType, values, config);
        }

        return RuntimeArrayFactory.CreateFromValues(elementType, values);
    }

    private static object CreateConcrete(Type concreteType, Type elementType, List<object?> values, AlderConfig? config)
    {
        // Typed dispatch: create empty instance + add elements
        if (config != null && config.TryGetDispatch(concreteType, out var dispatch))
            return CreateViaDispatch(dispatch, elementType, values);

        // JIT fallback: MakeGenericMethod
        return CreateCoreGenericMethod.MakeGenericMethod(concreteType, elementType).Invoke(null, [values])!;
    }

    private static object CreateViaDispatch(Aot.ITypedDispatch dispatch, Type elementType, List<object?> values)
    {
        if (!dispatch.TryCreate([], out var instance) || instance == null)
            throw new AlderException(DiagnosticDescriptors.NoMatchingConstructor, dispatch.Type.Name, "0");

        var convertTarget = Nullable.GetUnderlyingType(elementType) ?? elementType;
        foreach (var value in values)
        {
            object? converted;
            if (value == null)
                converted = null;
            else if (value.GetType() == convertTarget || value.GetType() == elementType)
                converted = value;
            else
                converted = Convert.ChangeType(value, convertTarget);

            dispatch.TryInvoke("Add", instance, [converted], out _);
        }
        return instance;
    }

    private static object? TryCreateViaRegisteredCollections(AlderConfig config, Type elementType, List<object?> values)
    {
        // In AOT, we can't MakeGenericType. Walk registered types to find a List<T> match.
        if (config.TypeDispatch == null) return null;

        foreach (var kvp in config.TypeDispatch)
        {
            var regType = kvp.Key;
            if (!regType.IsGenericType) continue;
            if (regType.GetGenericTypeDefinition() != typeof(List<>)) continue;
            if (regType.GetGenericArguments()[0] != elementType) continue;
            return CreateViaDispatch(kvp.Value, elementType, values);
        }

        return null;
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
