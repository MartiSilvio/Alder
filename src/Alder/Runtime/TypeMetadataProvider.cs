using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

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

    private readonly record struct PropertyLookupKey(
        [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type Type,
        string Name,
        BindingFlags Flags);

    private readonly record struct FieldLookupKey(
        [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type Type,
        string Name,
        BindingFlags Flags);

    private readonly record struct PropertiesLookupKey(
        [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type Type,
        BindingFlags Flags);

    private readonly record struct MethodLookupKey(
        [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type Type,
        string Name,
        BindingFlags Flags);

    private readonly record struct ConstructorLookupKey(
        [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type Type,
        BindingFlags Flags);

    internal TypeMetadataProvider()
    {
    }

    public PropertyInfo? GetProperty(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type, string name, BindingFlags flags)
    {
        var key = new PropertyLookupKey(type, name, flags);
        return _propertyCache.GetOrAdd(key, static k => ReflectionRuntime.FindProperty(k.Type, k.Name, k.Flags));
    }

    public FieldInfo? GetField(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type, string name, BindingFlags flags)
    {
        var key = new FieldLookupKey(type, name, flags);
        return _fieldCache.GetOrAdd(key, static k => ReflectionRuntime.FindField(k.Type, k.Name, k.Flags));
    }

    public PropertyInfo[] GetProperties(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type, BindingFlags flags)
    {
        var key = new PropertiesLookupKey(type, flags);
        return _propertiesCache.GetOrAdd(key, static k => ReflectionRuntime.GetProperties(k.Type, k.Flags));
    }

    public MethodInfo[] GetMethods(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type, string name, BindingFlags flags)
    {
        var key = new MethodLookupKey(type, name, flags);
        return _methodsCache.GetOrAdd(key, static k =>
        {
            var comparison = k.Flags.HasFlag(BindingFlags.IgnoreCase)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return ReflectionRuntime.GetMethods(k.Type, k.Flags)
                .Where(m => string.Equals(m.Name, k.Name, comparison))
                .ToArray();
        });
    }

    public ConstructorInfo[] GetConstructors(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type, BindingFlags flags)
    {
        var key = new ConstructorLookupKey(type, flags);
        return _constructorCache.GetOrAdd(key, static k => ReflectionRuntime.GetConstructors(k.Type, k.Flags));
    }

    public PropertyInfo? GetIndexer(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type)
    {
        return _indexerCache.GetOrAdd(type, ReflectionRuntime.FindIndexer);
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
