using CsEval.Diagnostics;
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

        // Handle static member access on Type objects (e.g., double.NaN)
        if (obj is Type staticType)
        {
            var staticBindingFlags = BindingFlags.Public | BindingFlags.Static;
            if (!options.IsCaseSensitive)
                staticBindingFlags |= BindingFlags.IgnoreCase;

            var staticProp = staticType.GetProperty(name, staticBindingFlags);
            if (staticProp != null)
                return TypeHelpers.CheckSandboxType(staticProp.GetValue(null), options.Sandbox);

            var staticField = staticType.GetField(name, staticBindingFlags);
            if (staticField != null)
                return TypeHelpers.CheckSandboxType(staticField.GetValue(null), options.Sandbox);

            // Check if this is a static method before falling through to instance members
            var staticMethods = staticType.GetMethods(staticBindingFlags);
            if (staticMethods.Any(m => string.Equals(m.Name, name, options.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)))
                return new StaticMethodRef(staticType, name);

            // Fall through to instance member access on the Type object itself
            // (e.g., typeof(int).Name accesses instance property Type.Name)
        }

        switch (obj)
        {
            case ModuleInfo module when module.Members.TryGetValue(name, out var memberInfo):
            {
                // For methods, defer resolution until invocation
                if (memberInfo is MethodInfo m)
                    return new ModuleMethodRef(module, context.ServiceProvider, m);

                // For properties/fields, resolve now to get value
                // Only resolve instance if member is not static
                var isStatic = memberInfo switch
                {
                    PropertyInfo p => p.GetMethod?.IsStatic ?? p.SetMethod?.IsStatic ?? false,
                    FieldInfo f => f.IsStatic,
                    _ => false
                };
                var instance = isStatic ? null : module.Resolve(context.ServiceProvider);
                var value = memberInfo switch
                {
                    PropertyInfo p => p.GetValue(instance),
                    FieldInfo f => f.GetValue(instance),
                    _ => throw new CsEvalException($"Unsupported member type '{memberInfo.GetType().Name}'")
                };
                return TypeHelpers.CheckSandboxType(value, options.Sandbox);
            }
            case ModuleInfo module:
                throw new CsEvalException(DiagnosticDescriptors.NoMemberOnType, module.Type.Name, name);
            case IDictionary<string, object?> dict when dict.TryGetValue(name, out var value):
                return TypeHelpers.CheckSandboxType(value, options.Sandbox);
            case IDictionary<string, object?> dict:
            {
                if (!options.IsCaseSensitive)
                {
                    foreach (var key in dict.Keys)
                    {
                        if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                            return TypeHelpers.CheckSandboxType(dict[key], options.Sandbox);
                    }
                }

                throw new CsEvalException(DiagnosticDescriptors.NoMemberOnType, obj.GetType().Name, name);
            }
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (!options.IsCaseSensitive)
            bindingFlags |= BindingFlags.IgnoreCase;

        var typeCache = context.TypeCache;
        var prop = typeCache.GetProperty(type, name, bindingFlags);
        if (prop != null)
            return TypeHelpers.CheckSandboxType(typeCache.GetPropertyValue(prop, obj), options.Sandbox);

        var field = typeCache.GetField(type, name, bindingFlags);
        if (field != null)
            return TypeHelpers.CheckSandboxType(field.GetValue(obj), options.Sandbox);

        return new MethodRef(obj, name);
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
            case string s when index != null:
            {
                var i = Convert.ToInt32(index);
                if (i < 0 || i >= s.Length)
                    throw new ArgumentOutOfRangeException("index", i,
                        "Index was out of range. Must be non-negative and less than the size of the collection.");
                var val = (object)s[i]; // Returns boxed char
                TypeHelpers.CheckSandboxType(val, options.Sandbox);
                return val;
            }
            case IList list when index is int i:
            {
                if (i < 0 || i >= list.Count) throw new ArgumentOutOfRangeException("index", i, "Index was out of range. Must be non-negative and less than the size of the collection.");
                var val = list[i];
                TypeHelpers.CheckSandboxType(val, options.Sandbox);
                return val;
            }
            case IList list:
                throw new ArgumentException($"Index must be an integer, got {index?.GetType().Name}", "index");
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

        throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, type.Name);
    }

    public static void SetMember(object? obj, string name, object? value, CsEvalOptions options, CsEvalContext context)
    {
        if (obj == null)
            throw new CsEvalException($"Cannot assign to property '{name}' on null");

        if (!options.Sandbox.AllowPropertySet)
            throw new CsEvalException($"Property assignment blocked by sandbox: {name} = ...");

        var caseInsensitive = !options.IsCaseSensitive;

        if (obj is IDictionary<string, object?> dict)
        {
            if (caseInsensitive)
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
        if (caseInsensitive)
            bindingFlags |= BindingFlags.IgnoreCase;

        var prop = context.TypeCache.GetProperty(type, name, bindingFlags);
        if (prop != null)
        {
            if (!prop.CanWrite)
                throw new CsEvalException(DiagnosticDescriptors.ReadonlyAssignment);
            prop.SetValue(obj, value);
            return;
        }

        var field = context.TypeCache.GetField(type, name, bindingFlags);
        if (field != null)
        {
            if (field.IsInitOnly)
                throw new CsEvalException(DiagnosticDescriptors.ReadonlyAssignment);
            field.SetValue(obj, value);
            return;
        }

        throw new CsEvalException(DiagnosticDescriptors.NoMemberOnType, type.Name, name);
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
                if (i < 0 || i >= list.Count) throw new ArgumentOutOfRangeException("index", i, "Index was out of range. Must be non-negative and less than the size of the collection.");

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
                throw new ArgumentException($"Index must be an integer, got {index?.GetType().Name}", "index");
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
                throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, type.Name);
            }
        }

        throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, type.Name);
    }

    internal static object? ConvertChangeType(object? value, Type targetType)
    {
        if (value == null) return null;
        if (targetType.IsInstanceOfType(value)) return value;
        return Convert.ChangeType(value, targetType);
    }
}
