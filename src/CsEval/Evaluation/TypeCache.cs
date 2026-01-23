using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace CsEval.Evaluation;

/// <summary>
/// Thread-safe cache for reflection lookups to avoid repeated GetProperty/GetField/GetMethods calls.
/// Uses static ConcurrentDictionary for global caching across all Evaluator instances.
/// Includes compiled property getters for improved performance over PropertyInfo.GetValue().
/// </summary>
internal static class TypeCache
{
    private static readonly ConcurrentDictionary<(Type, string, BindingFlags), PropertyInfo?> PropertyCache = new();
    private static readonly ConcurrentDictionary<(Type, string, BindingFlags), FieldInfo?> FieldCache = new();
    private static readonly ConcurrentDictionary<(Type, BindingFlags), PropertyInfo[]> PropertiesCache = new();
    private static readonly ConcurrentDictionary<(Type, string, BindingFlags), MethodInfo[]> MethodsCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> IndexerCache = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> CompiledGetters = new();

    public static PropertyInfo? GetProperty(Type type, string name, BindingFlags flags)
    {
        var key = (type, name, flags);
        return PropertyCache.GetOrAdd(key, k => k.Item1.GetProperty(k.Item2, k.Item3));
    }

    public static FieldInfo? GetField(Type type, string name, BindingFlags flags)
    {
        var key = (type, name, flags);
        return FieldCache.GetOrAdd(key, k => k.Item1.GetField(k.Item2, k.Item3));
    }

    public static PropertyInfo[] GetProperties(Type type, BindingFlags flags)
    {
        var key = (type, flags);
        return PropertiesCache.GetOrAdd(key, k => k.Item1.GetProperties(k.Item2));
    }

    public static MethodInfo[] GetMethods(Type type, string name, BindingFlags flags)
    {
        var key = (type, name, flags);
        return MethodsCache.GetOrAdd(key, k =>
            k.Item1.GetMethods(k.Item3)
                .Where(m => string.Equals(m.Name, k.Item2, StringComparison.OrdinalIgnoreCase))
                .ToArray());
    }

    public static PropertyInfo? GetIndexer(Type type)
    {
        return IndexerCache.GetOrAdd(type, t => t.GetProperty("Item"));
    }

    /// <summary>
    /// Gets or creates a compiled getter delegate for the property.
    /// </summary>
    public static Func<object, object?> GetCompiledGetter(PropertyInfo property)
    {
        return CompiledGetters.GetOrAdd(property, CompileGetter);
    }

    /// <summary>
    /// Gets the property value using a compiled getter for better performance.
    /// Falls back to PropertyInfo.GetValue() only if compilation fails.
    /// </summary>
    public static object? GetPropertyValue(PropertyInfo property, object instance)
    {
        var getter = GetCompiledGetter(property);
        return getter(instance);
    }

    private static Func<object, object?> CompileGetter(PropertyInfo property)
    {
        try
        {
            // Parameter: object instance
            var instanceParam = Expression.Parameter(typeof(object), "instance");

            // Cast to the declaring type: (DeclaringType)instance
            var castInstance = Expression.Convert(instanceParam, property.DeclaringType!);

            // Property access: ((DeclaringType)instance).PropertyName
            var propertyAccess = Expression.Property(castInstance, property);

            // Box value types: (object)propertyValue
            var boxedResult = Expression.Convert(propertyAccess, typeof(object));

            // Compile: instance => (object)((DeclaringType)instance).PropertyName
            var lambda = Expression.Lambda<Func<object, object?>>(boxedResult, instanceParam);
            return lambda.Compile();
        }
        catch
        {
            // Fallback to reflection if compilation fails (e.g., for some edge cases)
            return obj => property.GetValue(obj);
        }
    }

    /// <summary>
    /// Clears all cached reflection data. Useful for testing or when types are reloaded.
    /// </summary>
    public static void Clear()
    {
        PropertyCache.Clear();
        FieldCache.Clear();
        PropertiesCache.Clear();
        MethodsCache.Clear();
        IndexerCache.Clear();
        CompiledGetters.Clear();
    }
}
