using CsEval.Interpretation;

namespace CsEval.Runtime;

/// <summary>
/// Property, field, and index access operations.
/// </summary>
public static class MemberAccess
{
    public static object? GetMember(object? obj, string name, CsEvalOptions options, bool nullSafe, CsEvalContext context)
    {
        if (nullSafe && obj == null)
            return null;

        if (obj == null)
            throw new CsEvalException($"Cannot access property '{name}' on null");

        if (!options.Sandbox.AllowPropertyRead)
            throw new CsEvalException($"Property access blocked by sandbox: {name}");

        if (obj is CsEvalEngine.ModuleResolver resolver)
        {
            if (resolver.Members.TryGetValue(name, out var memberInfo))
            {
                var instance = resolver.Resolve();
                var value = memberInfo switch
                {
                    MethodInfo m => new ModuleMethodRef(resolver, m),
                    PropertyInfo p => p.GetValue(p.GetMethod!.IsStatic ? null : instance),
                    FieldInfo f => f.GetValue(f.IsStatic ? null : instance),
                    _ => throw new CsEvalException($"Unsupported member type '{memberInfo.GetType().Name}'")
                };
                return TypeHelpers.CheckSandboxType(value, options.Sandbox);
            }
            throw new CsEvalException($"Member '{name}' not found on module '{resolver.Type.Name}'");
        }

        if (obj is IDictionary<string, object?> dict)
        {
            if (dict.TryGetValue(name, out var value))
                return TypeHelpers.CheckSandboxType(value, options.Sandbox);

            if (options.IgnoreCase)
            {
                foreach (var key in dict.Keys)
                {
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        return TypeHelpers.CheckSandboxType(dict[key], options.Sandbox);
                }
            }

            throw new CsEvalException($"Property '{name}' not found");
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (options.IgnoreCase)
            bindingFlags |= BindingFlags.IgnoreCase;

        var typeCache = context.TypeCache;
        var prop = typeCache.GetProperty(type, name, bindingFlags);
        if (prop != null)
            return TypeHelpers.CheckSandboxType(typeCache.GetPropertyValue(prop, obj), options.Sandbox);

        var field = typeCache.GetField(type, name, bindingFlags);
        if (field != null)
            return TypeHelpers.CheckSandboxType(field.GetValue(obj), options.Sandbox);

        throw new CsEvalException($"Property '{name}' not found on type '{type.Name}'");
    }

    public static object? GetIndex(object? obj, object? index, CsEvalOptions options)
    {
        if (obj == null)
            throw new CsEvalException("Cannot index null");

        if (obj is IDictionary<string, object?> dict)
        {
            var key = index?.ToString() ?? "";
            var val = dict.TryGetValue(key, out var v) ? v : null;
            TypeHelpers.CheckSandboxType(val, options.Sandbox);
            return val;
        }

        if (obj is System.Collections.IList list)
        {
            if (index is int i)
            {
                if (i < 0 || i >= list.Count) throw new CsEvalException($"Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')");
                var val = list[i];
                TypeHelpers.CheckSandboxType(val, options.Sandbox);
                return val;
            }
            throw new CsEvalException($"Hashtable/List index must be an integer, got {index?.GetType().Name}");
        }

        var type = obj.GetType();
        var indexer = type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);

        if (indexer != null && indexer.GetIndexParameters().Length == 1)
        {
            try
            {
                var paramType = indexer.GetIndexParameters()[0].ParameterType;
                var safeIndex = ConvertChangeType(index, paramType);
                var val = indexer.GetValue(obj, new[] { safeIndex });
                TypeHelpers.CheckSandboxType(val, options.Sandbox);
                return val;
            }
            catch (CsEvalException) { throw; }
            catch (Exception ex)
            {
                throw new CsEvalException($"Indexer access failed: {ex.Message}");
            }
        }

        throw new CsEvalException($"Type '{type.Name}' cannot be indexed");
    }

    public static void SetIndex(object? obj, object? index, object? value)
    {
        if (obj == null)
            throw new CsEvalException("Cannot index assign null");

        if (obj is IDictionary<string, object?> dict)
        {
            var key = index?.ToString() ?? "";
            dict[key] = value;
            return;
        }

        if (obj is System.Collections.IList list)
        {
            if (index is int i)
            {
                if (i < 0 || i >= list.Count) throw new CsEvalException($"Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')");

                if (list.GetType().IsGenericType)
                {
                    var elementType = list.GetType().GetGenericArguments()[0];
                    list[i] = ConvertChangeType(value, elementType);
                }
                else
                {
                    list[i] = value;
                }
                return;
            }
            throw new CsEvalException($"Hashtable/List index must be an integer, got {index?.GetType().Name}");
        }

        var type = obj.GetType();
        var indexer = type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);

        if (indexer != null && indexer.GetIndexParameters().Length == 1 && indexer.CanWrite)
        {
            try
            {
                var paramType = indexer.GetIndexParameters()[0].ParameterType;
                var safeIndex = ConvertChangeType(index, paramType);
                indexer.SetValue(obj, value, new[] { safeIndex });
                return;
            }
            catch
            {
                throw new CsEvalException($"Cannot set index on type '{type.Name}'");
            }
        }

        throw new CsEvalException($"Type '{type.Name}' does not support index assignment");
    }

    internal static object? ConvertChangeType(object? value, Type targetType)
    {
        if (value == null) return null;
        if (targetType.IsInstanceOfType(value)) return value;
        return Convert.ChangeType(value, targetType);
    }
}
