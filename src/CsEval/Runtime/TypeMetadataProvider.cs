using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace CsEval.Runtime;

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
    private readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> _compiledGetters = new();

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

    public PropertyInfo? GetIndexer(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type)
    {
        return _indexerCache.GetOrAdd(type, ReflectionRuntime.FindIndexer);
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

        var param = Expression.Parameter(typeof(object), "instance");

        if (getter.IsStatic)
        {
            var call = Expression.Call(getter);
            var boxed = Expression.Convert(call, typeof(object));
            return Expression.Lambda<Func<object, object?>>(boxed, param).Compile();
        }

        var declaringType = getter.DeclaringType!;
        System.Linq.Expressions.Expression typedInstance = declaringType.IsValueType
            ? Expression.Unbox(param, declaringType)
            : Expression.Convert(param, declaringType);
        var propertyAccess = Expression.Property(typedInstance, property);
        var boxedResult = Expression.Convert(propertyAccess, typeof(object));
        return Expression.Lambda<Func<object, object?>>(boxedResult, param).Compile();
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
        _compiledGetters.Clear();
    }
}
