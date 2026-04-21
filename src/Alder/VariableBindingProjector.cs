using System.Collections.Concurrent;
using Alder.Runtime;

namespace Alder;

internal static class VariableBindingProjector
{
    private static readonly ConcurrentDictionary<Type, (string Name, Type PropertyType, Func<object, object?> Getter)[]> AccessorCache = new();
    private static readonly MethodInfo? WrapGetterMethod =
        typeof(VariableBindingProjector).GetMethod(nameof(WrapGetter), BindingFlags.NonPublic | BindingFlags.Static);

    internal static (string Name, object? Value, Type Type)[] ProjectTypedVariables(object obj)
    {
        var accessors = AccessorCache.GetOrAdd(obj.GetType(), static t =>
        {
            var props = RuntimeTypeIntrospection.GetProperties(t, BindingFlags.Public | BindingFlags.Instance);
            var result = new (string Name, Type PropertyType, Func<object, object?> Getter)[props.Length];
            for (var i = 0; i < props.Length; i++)
            {
                var prop = props[i];
                var getter = prop.GetGetMethod();
                if (getter == null || WrapGetterMethod == null)
                {
                    var property = prop;
                    result[i] = (prop.Name, prop.PropertyType, value => property.GetValue(value));
                    continue;
                }

                var boxedGetter = (Func<object, object?>)RuntimeGenericClosure
                    .CloseMethod(WrapGetterMethod, [t, prop.PropertyType])
                    .Invoke(null, [getter])!;
                result[i] = (prop.Name, prop.PropertyType, boxedGetter);
            }

            return result;
        });

        var projected = new (string Name, object? Value, Type Type)[accessors.Length];
        for (var i = 0; i < accessors.Length; i++)
            projected[i] = (accessors[i].Name, accessors[i].Getter(obj), accessors[i].PropertyType);
        return projected;
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
                foreach (var (name, value, _) in ProjectTypedVariables(variable))
                    result[name] = value;
            }
        }

        return result;
    }

    private static Func<object, object?> WrapGetter<TOwner, TProp>(MethodInfo getMethod)
    {
        var typed = (Func<TOwner, TProp>)Delegate.CreateDelegate(typeof(Func<TOwner, TProp>), getMethod);
        return value => typed((TOwner)value);
    }

    private static bool IsSimpleType(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
        || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan)
        || type == typeof(Guid) || Nullable.GetUnderlyingType(type) != null;
}
