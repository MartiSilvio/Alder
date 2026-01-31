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

        switch (obj)
        {
            case CsEvalEngine.ModuleResolver resolver when resolver.Members.TryGetValue(name, out var memberInfo):
            {
                // For methods, defer resolution until invocation
                if (memberInfo is MethodInfo m)
                    return new ModuleMethodRef(resolver, m);

                // For properties/fields, resolve now to get value
                var instance = resolver.Resolve();
                var value = memberInfo switch
                {
                    PropertyInfo p => p.GetValue(p.GetMethod!.IsStatic ? null : instance),
                    FieldInfo f => f.GetValue(f.IsStatic ? null : instance),
                    _ => throw new CsEvalException($"Unsupported member type '{memberInfo.GetType().Name}'")
                };
                return TypeHelpers.CheckSandboxType(value, options.Sandbox);
            }
            case CsEvalEngine.ModuleResolver resolver:
                throw new CsEvalException($"Member '{name}' not found on module '{resolver.Type.Name}'");
            case IDictionary<string, object?> dict when dict.TryGetValue(name, out var value):
                return TypeHelpers.CheckSandboxType(value, options.Sandbox);
            case IDictionary<string, object?> dict:
            {
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
        switch (obj)
        {
            case null:
                throw new CsEvalException("Cannot index null");
            case IDictionary<string, object?> dict:
            {
                var key = index?.ToString() ?? "";
                var val = dict.TryGetValue(key, out var v) ? v : null;
                TypeHelpers.CheckSandboxType(val, options.Sandbox);
                return val;
            }
            case IList list when index is int i:
            {
                if (i < 0 || i >= list.Count) throw new CsEvalException($"Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')");
                var val = list[i];
                TypeHelpers.CheckSandboxType(val, options.Sandbox);
                return val;
            }
            case IList list:
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

    public static void SetMember(object? obj, string name, object? value, CsEvalOptions options, CsEvalContext context)
    {
        if (obj == null)
            throw new CsEvalException($"Cannot assign to property '{name}' on null");

        if (!options.Sandbox.AllowPropertySet)
            throw new CsEvalException($"Property assignment blocked by sandbox: {name} = ...");

        var ignoreCase = options.IgnoreCase;

        if (obj is IDictionary<string, object?> dict)
        {
            if (ignoreCase)
            {
                foreach (var key in dict.Keys)
                {
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        dict[key] = value;
                        return;
                    }
                }
            }
            dict[name] = value;
            return;
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (ignoreCase)
            bindingFlags |= BindingFlags.IgnoreCase;

        var prop = context.TypeCache.GetProperty(type, name, bindingFlags);
        if (prop != null)
        {
            if (!prop.CanWrite)
                throw new CsEvalException($"Property '{name}' is read-only");
            prop.SetValue(obj, value);
            return;
        }

        var field = context.TypeCache.GetField(type, name, bindingFlags);
        if (field != null)
        {
            if (field.IsInitOnly)
                throw new CsEvalException($"Field '{name}' is read-only");
            field.SetValue(obj, value);
            return;
        }

        throw new CsEvalException($"Property '{name}' not found on type '{type.Name}'");
    }

    public static void SetIndex(object? obj, object? index, object? value)
    {
        switch (obj)
        {
            case null:
                throw new CsEvalException("Cannot index assign null");
            case IDictionary<string, object?> dict:
            {
                var key = index?.ToString() ?? "";
                dict[key] = value;
                return;
            }
            case IList list when index is int i:
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
            case IList list:
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
