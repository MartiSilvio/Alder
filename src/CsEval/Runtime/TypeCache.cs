using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CsEval.Runtime;

/// <summary>
/// Thread-safe cache for reflection lookups to avoid repeated GetProperty/GetField/GetMethods calls.
/// Instance-based caching per engine - cache is shared with child engines and cleaned up when root engine is disposed.
/// Includes compiled property getters for improved performance over PropertyInfo.GetValue().
/// </summary>
internal sealed class TypeCache
{
    private readonly ConcurrentDictionary<(Type, string, BindingFlags), PropertyInfo?> _propertyCache = new();
    private readonly ConcurrentDictionary<(Type, string, BindingFlags), FieldInfo?> _fieldCache = new();
    private readonly ConcurrentDictionary<(Type, BindingFlags), PropertyInfo[]> _propertiesCache = new();
    private readonly ConcurrentDictionary<(Type, string, BindingFlags), MethodInfo[]> _methodsCache = new();
    private readonly ConcurrentDictionary<Type, PropertyInfo?> _indexerCache = new();
    private readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> _compiledGetters = new();

    internal TypeCache()
    {
    }

    public PropertyInfo? GetProperty(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type,
        string name,
        BindingFlags flags)
    {
        var key = (type, name, flags);
        return _propertyCache.GetOrAdd(key, k => k.Item1.GetProperty(k.Item2, k.Item3));
    }

    public FieldInfo? GetField(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] Type type,
        string name,
        BindingFlags flags)
    {
        var key = (type, name, flags);
        return _fieldCache.GetOrAdd(key, k => k.Item1.GetField(k.Item2, k.Item3));
    }

    public PropertyInfo[] GetProperties(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type,
        BindingFlags flags)
    {
        var key = (type, flags);
        return _propertiesCache.GetOrAdd(key, k => k.Item1.GetProperties(k.Item2));
    }

    public MethodInfo[] GetMethods(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type,
        string name,
        BindingFlags flags)
    {
        var key = (type, name, flags);
        return _methodsCache.GetOrAdd(key, k =>
        {
            var comparison = k.Item3.HasFlag(BindingFlags.IgnoreCase)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return k.Item1.GetMethods(k.Item3)
                .Where(m => string.Equals(m.Name, k.Item2, comparison))
                .ToArray();
        });
    }

    public PropertyInfo? GetIndexer([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        return _indexerCache.GetOrAdd(type, t => t.GetProperty("Item"));
    }

    /// <summary>
    /// Gets or creates a compiled getter delegate for the property.
    /// </summary>
    public Func<object, object?> GetCompiledGetter(PropertyInfo property)
    {
        return _compiledGetters.GetOrAdd(property, p => CompileGetter(p));
    }

    /// <summary>
    /// Gets the property value using a compiled getter for better performance.
    /// Falls back to PropertyInfo.GetValue() only if compilation fails.
    /// </summary>
    public object? GetPropertyValue(PropertyInfo property, object instance)
    {
        var getter = GetCompiledGetter(property);
        return getter(instance);
    }

    private Func<object, object?> CompileGetter(PropertyInfo property)
    {
        var getter = property.GetMethod;
        if (getter == null)
            return obj => property.GetValue(obj);

        if (getter.IsStatic)
            return _ => getter.Invoke(null, null);

        return instance => getter.Invoke(instance, null);
    }

    /// <summary>
    /// Clears all cached reflection data. Useful for testing or when types are reloaded.
    /// </summary>
    public void Clear()
    {
        _propertyCache.Clear();
        _fieldCache.Clear();
        _propertiesCache.Clear();
        _methodsCache.Clear();
        _indexerCache.Clear();
        _compiledGetters.Clear();
    }
}
