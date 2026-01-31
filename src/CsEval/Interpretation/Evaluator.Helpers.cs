using CsEval.Runtime;

namespace CsEval.Interpretation;

public sealed partial class Evaluator
{
    /// <summary>
    /// Guards against reflection type leaks. Throws if value is a forbidden reflection type.
    /// </summary>
    private static object? GuardReflectionLeak(object? value, string context)
    {
        if (value == null) return null;

        var type = value.GetType();
        if (TypeHelpers.IsForbiddenReflectionType(type))
        {
            throw new CsEvalException($"Access to reflection types is not allowed: {type.Name} ({context})");
        }

        return value;
    }

    private object? GetMember(object obj, string name)
    {
        if (obj is CsEvalEngine.ModuleResolver resolver)
        {
            if (resolver.Members.TryGetValue(name, out var member))
            {
                return member switch
                {
                    MethodInfo m => new ModuleMethodRef(resolver, m),
                    PropertyInfo p => GuardReflectionLeak(_context.TypeCache.GetPropertyValue(p, resolver.Resolve()!), $"property {name}"),
                    _ => throw new CsEvalException($"Unsupported member type '{member.GetType().Name}'")
                };
            }
            throw new CsEvalException($"Member '{name}' not found on module '{resolver.Type.Name}'");
        }

        if (!_options.Sandbox.AllowPropertyRead)
            throw new CsEvalException($"Property access blocked by sandbox: {name}");

        var ignoreCase = _options.IgnoreCase;

        if (obj is IDictionary<string, object?> dict)
        {
            if (dict.TryGetValue(name, out var value))
                return GuardReflectionLeak(value, $"property {name}");

            if (ignoreCase)
            {
                foreach (var key in dict.Keys)
                {
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        return GuardReflectionLeak(dict[key], $"property {name}");
                }
            }

            throw new CsEvalException($"Property '{name}' not found");
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (ignoreCase)
            bindingFlags |= BindingFlags.IgnoreCase;

        var prop = _context.TypeCache.GetProperty(type, name, bindingFlags);
        if (prop != null)
            return GuardReflectionLeak(_context.TypeCache.GetPropertyValue(prop, obj), $"property {name}");

        var field = _context.TypeCache.GetField(type, name, bindingFlags);
        if (field != null)
            return GuardReflectionLeak(field.GetValue(obj), $"field {name}");

        throw new CsEvalException($"Property '{name}' not found on type '{type.Name}'");
    }

    private object? GetIndex(object obj, object? index)
    {
        if (obj is IDictionary<string, object?> dict && index is string strKey)
        {
            if (dict.TryGetValue(strKey, out var value))
                return GuardReflectionLeak(value, $"index [{strKey}]");
            return null;
        }

        if (obj is IList list && index != null)
        {
            var idx = Convert.ToInt32(index);
            if (idx < 0 || idx >= list.Count)
                throw new CsEvalException($"Index {idx} out of range");
            return GuardReflectionLeak(list[idx], $"index [{idx}]");
        }

        var type = obj.GetType();
        var indexer = _context.TypeCache.GetIndexer(type);
        if (indexer != null)
            return GuardReflectionLeak(indexer.GetValue(obj, [index]), $"indexer access");

        throw new CsEvalException($"Cannot index type '{type.Name}'");
    }

    private void SetIndex(object obj, object? index, object? value)
    {
        if (!_options.Sandbox.AllowIndexSet)
            throw new CsEvalException($"Index assignment blocked by sandbox: [{index}] = ...");

        switch (obj)
        {
            case IDictionary<string, object?> dict when index is string strKey:
                dict[strKey] = value;
                return;
            case IList list when index != null:
            {
                var idx = Convert.ToInt32(index);
                if (idx < 0 || idx >= list.Count)
                    throw new CsEvalException($"Index {idx} out of range");
                list[idx] = value;
                return;
            }
        }

        var type = obj.GetType();
        var indexer = _context.TypeCache.GetIndexer(type);
        if (indexer != null && indexer.CanWrite)
        {
            indexer.SetValue(obj, value, [index]);
            return;
        }

        throw new CsEvalException($"Cannot set index on type '{type.Name}'");
    }

    private void SetMember(object obj, string name, object? value)
    {
        if (!_options.Sandbox.AllowPropertySet)
            throw new CsEvalException($"Property assignment blocked by sandbox: {name} = ...");

        var ignoreCase = _options.IgnoreCase;

        if (obj is IDictionary<string, object?> dict)
        {
            // For dictionaries, try to find existing key with case-insensitive match
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
            // If no match found or case-sensitive, use the provided name
            dict[name] = value;
            return;
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (ignoreCase)
            bindingFlags |= BindingFlags.IgnoreCase;

        var prop = _context.TypeCache.GetProperty(type, name, bindingFlags);
        if (prop != null)
        {
            if (!prop.CanWrite)
                throw new CsEvalException($"Property '{name}' is read-only");
            prop.SetValue(obj, value);
            return;
        }

        var field = _context.TypeCache.GetField(type, name, bindingFlags);
        if (field != null)
        {
            if (field.IsInitOnly)
                throw new CsEvalException($"Field '{name}' is read-only");
            field.SetValue(obj, value);
            return;
        }

        throw new CsEvalException($"Property '{name}' not found on type '{type.Name}'");
    }
}
