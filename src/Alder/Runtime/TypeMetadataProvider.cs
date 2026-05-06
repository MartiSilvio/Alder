using System.Collections.Concurrent;

namespace Alder.Runtime;

/// <summary>
/// Central metadata access service for runtime reflection queries.
/// Internally memoizes lookups per engine instance and shares state with child engines.
/// Includes compiled property getters for improved access performance.
/// </summary>
internal sealed class TypeMetadataProvider
{
    private readonly ConcurrentDictionary<PropertyLookupKey, PropertyInfo?> _propertyCache = new();
    private readonly ConcurrentDictionary<FieldLookupKey, FieldInfo?> _fieldCache = new();
    private readonly ConcurrentDictionary<PropertiesLookupKey, PropertyInfo[]> _propertiesCache = new();
    private readonly ConcurrentDictionary<MethodLookupKey, MethodInfo[]> _methodsCache = new();
    private readonly ConcurrentDictionary<Type, PropertyInfo?> _indexerCache = new();
    private readonly ConcurrentDictionary<ConstructorLookupKey, ConstructorInfo[]> _constructorCache = new();

    private readonly record struct PropertyLookupKey(Type Type, string Name, BindingFlags Flags);

    private readonly record struct FieldLookupKey(Type Type, string Name, BindingFlags Flags);

    private readonly record struct PropertiesLookupKey(Type Type, BindingFlags Flags);

    private readonly record struct MethodLookupKey(Type Type, string Name, BindingFlags Flags);

    private readonly record struct ConstructorLookupKey(Type Type, BindingFlags Flags);

    internal TypeMetadataProvider()
    {
    }

    public PropertyInfo? GetProperty(Type type, string name, BindingFlags flags)
    {
        var key = new PropertyLookupKey(type, name, flags);
        return _propertyCache.GetOrAdd(key, static k => RuntimeTypeIntrospection.FindProperty(k.Type, k.Name, k.Flags));
    }

    public FieldInfo? GetField(Type type, string name, BindingFlags flags)
    {
        var key = new FieldLookupKey(type, name, flags);
        return _fieldCache.GetOrAdd(key, static k => RuntimeTypeIntrospection.FindField(k.Type, k.Name, k.Flags));
    }

    public PropertyInfo[] GetProperties(Type type, BindingFlags flags)
    {
        var key = new PropertiesLookupKey(type, flags);
        return _propertiesCache.GetOrAdd(key, static k => RuntimeTypeIntrospection.GetProperties(k.Type, k.Flags));
    }

    public MethodInfo[] GetMethods(Type type, string name, BindingFlags flags)
    {
        var key = new MethodLookupKey(type, name, flags);
        return _methodsCache.GetOrAdd(key, static k =>
        {
            var comparison = k.Flags.HasFlag(BindingFlags.IgnoreCase)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return RuntimeTypeIntrospection.GetMethods(k.Type, k.Flags)
                .Where(m => string.Equals(m.Name, k.Name, comparison))
                .ToArray();
        });
    }

    public ConstructorInfo[] GetConstructors(Type type, BindingFlags flags)
    {
        var key = new ConstructorLookupKey(type, flags);
        return _constructorCache.GetOrAdd(key, static k => RuntimeTypeIntrospection.GetConstructors(k.Type, k.Flags));
    }

    public PropertyInfo? GetIndexer(Type type)
    {
        return _indexerCache.GetOrAdd(type, RuntimeTypeIntrospection.FindIndexer);
    }

    public object? GetPropertyValue(PropertyInfo property, object instance)
    {
        return property.GetValue(instance);
    }

    /// <summary>
    /// Clears all memoized metadata entries.
    /// </summary>
    public void Clear()
    {
        _propertyCache.Clear();
        _fieldCache.Clear();
        _propertiesCache.Clear();
        _methodsCache.Clear();
        _indexerCache.Clear();
        _constructorCache.Clear();
    }
}
