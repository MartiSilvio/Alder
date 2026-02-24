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

        // Handle namespace sentinel: accumulate path segments for FQN type resolution.
        // Example: NamespaceRef("System") + "Linq" -> NamespaceRef("System.Linq") or Type
        if (obj is NamespaceRef nsRef)
        {
            var accumulated = nsRef.Path + "." + name;

            // Try to resolve as a complete type name
            var resolvedType = context.TypeResolver.TryResolveType(accumulated);
            if (resolvedType != null)
                return resolvedType;

            // Check if it's still a valid namespace prefix
            if (context.TypeResolver.IsNamespaceOrPrefix(accumulated))
                return new NamespaceRef(accumulated);

            // Neither a type nor a namespace prefix -- this is an error
            throw new CsEvalException(DiagnosticDescriptors.TypeNotFound, accumulated);
        }

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

        // Module members are always accessible regardless of sandbox settings.
        // Check modules before the AllowPropertyRead guard so that module methods,
        // properties, and fields are never blocked by the sandbox.
        if (obj is ModuleInfo module)
        {
            if (module.Members.TryGetValue(name, out var memberInfo))
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
            throw new CsEvalException(DiagnosticDescriptors.NoMemberOnType, module.Type.Name, name);
        }

        // Sandbox guard: block property/field reads on variable objects when not allowed.
        // This check is intentionally placed AFTER module/namespace/type handling so that
        // modules, namespaces, and static type members are always accessible.
        if (!options.Sandbox.AllowPropertyRead)
            throw new CsEvalException($"Property access blocked by sandbox: {name}");

        switch (obj)
        {
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
                if (i < 0 && options.LanguageMode == LanguageMode.Extended)
                    i = s.Length + i;
                if (i < 0 || i >= s.Length)
                    throw new ArgumentOutOfRangeException("index", i,
                        "Index was out of range. Must be non-negative and less than the size of the collection.");
                var val = (object)s[i]; // Returns boxed char
                TypeHelpers.CheckSandboxType(val, options.Sandbox);
                return val;
            }
            case IList list when index is int i:
            {
                var idx = i;
                if (idx < 0 && options.LanguageMode == LanguageMode.Extended)
                    idx = list.Count + idx;
                if (idx < 0 || idx >= list.Count) throw new ArgumentOutOfRangeException("index", idx, "Index was out of range. Must be non-negative and less than the size of the collection.");
                var val = list[idx];
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

    public static void SetIndex(object? obj, object? index, object? value, CsEvalOptions? options = null)
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
                var idx = i;
                if (idx < 0 && options?.LanguageMode == LanguageMode.Extended)
                    idx = list.Count + idx;
                if (idx < 0 || idx >= list.Count) throw new ArgumentOutOfRangeException("index", idx, "Index was out of range. Must be non-negative and less than the size of the collection.");

                if (list.GetType().IsGenericType)
                {
                    var elementType = list.GetType().GetGenericArguments()[0];
                    list[idx] = ConvertChangeType(value, elementType);
                }
                else
                {
                    list[idx] = value;
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

    /// <summary>
    /// Python-style slice: obj[start:end] where start is inclusive, end is exclusive.
    /// Omitted start defaults to 0, omitted end defaults to length.
    /// Out-of-bounds indices are clamped (Python behavior).
    /// Returns same type as input: T[] -> T[], List&lt;T&gt; -> List&lt;T&gt;, string -> string.
    /// </summary>
    public static object? GetSlice(object? obj, object? start, object? end, CsEvalOptions options)
    {
        if (obj == null)
            throw new CsEvalException("Cannot slice null");

        switch (obj)
        {
            case string s:
            {
                int startIdx = start != null ? Convert.ToInt32(start) : 0;
                int endIdx = end != null ? Convert.ToInt32(end) : s.Length;
                ClampSliceIndices(ref startIdx, ref endIdx, s.Length);
                if (startIdx >= endIdx) return (object)"";
                return (object)s.Substring(startIdx, endIdx - startIdx);
            }
            case Array arr:
            {
                int startIdx = start != null ? Convert.ToInt32(start) : 0;
                int endIdx = end != null ? Convert.ToInt32(end) : arr.Length;
                ClampSliceIndices(ref startIdx, ref endIdx, arr.Length);
                int count = Math.Max(0, endIdx - startIdx);
                var elementType = arr.GetType().GetElementType()!;
                var result = Array.CreateInstance(elementType, count);
                if (count > 0)
                    Array.Copy(arr, startIdx, result, 0, count);
                return result;
            }
            case IList list:
            {
                int startIdx = start != null ? Convert.ToInt32(start) : 0;
                int endIdx = end != null ? Convert.ToInt32(end) : list.Count;
                ClampSliceIndices(ref startIdx, ref endIdx, list.Count);
                int count = Math.Max(0, endIdx - startIdx);
                // Preserve List<T> type
                var listType = list.GetType();
                if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = listType.GetGenericArguments()[0];
                    var resultList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
                    for (int i = startIdx; i < startIdx + count; i++)
                        resultList.Add(list[i]);
                    return resultList;
                }
                // Fallback: return object[]
                var fallback = new object?[count];
                for (int i = 0; i < count; i++)
                    fallback[i] = list[startIdx + i];
                return fallback;
            }
            default:
                throw new CsEvalException($"Cannot slice type '{obj.GetType().Name}' -- slicing requires an array, List<T>, or string");
        }
    }

    /// <summary>
    /// Clamps slice indices to valid range (Python behavior).
    /// Negative indices are converted: -1 -> length-1, etc.
    /// Out-of-range indices are clamped to [0, length].
    /// </summary>
    private static void ClampSliceIndices(ref int start, ref int end, int length)
    {
        // Handle negative indices (Python semantics)
        if (start < 0) start = Math.Max(0, length + start);
        if (end < 0) end = Math.Max(0, length + end);

        // Clamp to valid range
        start = Math.Clamp(start, 0, length);
        end = Math.Clamp(end, 0, length);
    }

    internal static object? ConvertChangeType(object? value, Type targetType)
    {
        if (value == null) return null;
        if (targetType.IsInstanceOfType(value)) return value;
        return Convert.ChangeType(value, targetType);
    }
}
