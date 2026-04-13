using System.Collections.Concurrent;

namespace Alder;

public sealed partial class AlderEngine
{
    private static readonly ConcurrentDictionary<Type, (string Name, Type PropertyType, Func<object, object?> Getter)[]> VariableAccessorCache = new();
    private static readonly MethodInfo? WrapGetterMethod =
        typeof(AlderEngine).GetMethod(nameof(WrapGetter), BindingFlags.NonPublic | BindingFlags.Static);

    private static (string Name, object? Value, Type Type)[] ToTypedVariables(object obj)
    {
        var accessors = VariableAccessorCache.GetOrAdd(obj.GetType(), static t =>
        {
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var result = new (string Name, Type PropertyType, Func<object, object?> Getter)[props.Length];
            for (var i = 0; i < props.Length; i++)
            {
                var prop = props[i];
                var getter = prop.GetGetMethod();
                if (getter == null || WrapGetterMethod == null)
                {
                    var p = prop;
                    result[i] = (prop.Name, prop.PropertyType, o => p.GetValue(o));
                    continue;
                }

                var boxed = (Func<object, object?>)WrapGetterMethod
                    .MakeGenericMethod(t, prop.PropertyType)
                    .Invoke(null, [getter])!;
                result[i] = (prop.Name, prop.PropertyType, boxed);
            }
            return result;
        });

        var result = new (string Name, object? Value, Type Type)[accessors.Length];
        for (var i = 0; i < accessors.Length; i++)
            result[i] = (accessors[i].Name, accessors[i].Getter(obj), accessors[i].PropertyType);
        return result;
    }

    private static Func<object, object?> WrapGetter<TOwner, TProp>(MethodInfo getMethod)
    {
        var typed = (Func<TOwner, TProp>)Delegate.CreateDelegate(typeof(Func<TOwner, TProp>), getMethod);
        return obj => typed((TOwner)obj);
    }

    internal static Dictionary<string, object?> BuildPositionalVariables(object?[] variables)
    {
        var result = new Dictionary<string, object?>();
        for (var i = 0; i < variables.Length; i++)
        {
            var variable = variables[i];
            result[$"__p{i}"] = variable;

            if (variable is IDictionary<string, object?> dict)
            {
                foreach (var (key, value) in dict)
                    result[key] = value;
            }
            else if (variable != null && !IsSimpleType(variable.GetType()))
            {
                foreach (var (name, value, _) in ToTypedVariables(variable))
                    result[name] = value;
            }
        }
        return result;
    }

    private static bool IsSimpleType(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
        || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan)
        || type == typeof(Guid) || Nullable.GetUnderlyingType(type) != null;

    private Dictionary<string, object?> CollectEngineVariables()
    {
        var variables = new Dictionary<string, object?>(_config.Comparer);

        lock (_contextInitLock)
        {
            foreach (var (name, pending) in _pendingVariables)
            {
                variables[name] = pending.Value;
            }
        }

        if (_context != null)
        {
            foreach (var (name, value) in _context.GetAllVisible())
            {
                variables[name] = value;
            }
        }

        return variables;
    }
}
