using System.Reflection;
using System.Runtime.CompilerServices;
using Alder.Diagnostics;
using Alder.Runtime.Extensions;

namespace Alder.Runtime;

/// <summary>
/// Property, field, and index access operations.
/// </summary>
internal static class MemberAccess
{
    public static object? GetMember(object? obj, string name, AlderConfig config, bool nullSafe, AlderContext context)
    {
        if (nullSafe && obj == null)
            return null;

        if (obj == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "property", name);

        if (config.LanguageMode == LanguageMode.Extended &&
            DateArithmeticSugar.TryResolveTimeSpanUnit(obj, name, config.IsCaseSensitive, out var timeSpan))
        {
            return timeSpan;
        }

        // Handle namespace sentinel: accumulate path segments for FQN type resolution.
        // Example: NamespaceRef("System") + "Linq" -> NamespaceRef("System.Linq") or Type
        if (obj is NamespaceRef nsRef)
        {
            var accumulated = nsRef.Path + "." + name;

            // Try to resolve as a complete type name
            var resolvedType = context.TypeResolver.TryResolveType(accumulated);
            if (resolvedType != null)
            {
                if (!config.Security.IsTrusted && !config.Security.IsTypeAllowed(resolvedType))
                    throw new AlderException(DiagnosticDescriptors.SandboxTypeBlocked, resolvedType.Name);
                return resolvedType;
            }

            // Check if it's still a valid namespace prefix
            if (context.TypeResolver.IsNamespaceOrPrefix(accumulated))
                return new NamespaceRef(accumulated);

            // Neither a type nor a namespace prefix -- this is an error
            throw new AlderException(DiagnosticDescriptors.TypeNotFound, accumulated);
        }

        if (obj is Type staticType)
        {
            if (TypedDispatchHelper.TryGetStaticMember(config, staticType, name, out var aotStaticValue))
                return aotStaticValue;

            var staticTypeCache = context.TypeMetadata;
            var staticBindingFlags = BindingFlags.Public | BindingFlags.Static;
            if (!config.IsCaseSensitive)
                staticBindingFlags |= BindingFlags.IgnoreCase;

            var staticProp = staticTypeCache.GetProperty(staticType, name, staticBindingFlags);
            if (staticProp != null)
                return TypeHelpers.GuardReflectionLeak(staticProp.GetValue(null), $"static property {name}");

            var staticField = staticTypeCache.GetField(staticType, name, staticBindingFlags);
            if (staticField != null)
                return TypeHelpers.GuardReflectionLeak(staticField.GetValue(null), $"static field {name}");

            var staticMethods = staticTypeCache.GetMethods(staticType, name, staticBindingFlags);
            if (staticMethods.Length > 0)
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
                    PropertyInfo p => context.TypeMetadata.GetPropertyValue(p, instance!),
                    FieldInfo f => f.GetValue(instance),
                    _ => throw new AlderException(DiagnosticDescriptors.UnsupportedMemberType, memberInfo.GetType().Name)
                };
                return TypeHelpers.GuardReflectionLeak(value, $"module member {name}");
            }
            throw new AlderException(DiagnosticDescriptors.NoMemberOnType, module.Type.Name, name);
        }

        switch (obj)
        {
            case IDictionary<string, object?> dict when dict.TryGetValue(name, out var value):
                return TypeHelpers.GuardReflectionLeak(value, $"property {name}");
            case IDictionary<string, object?> dict:
            {
                if (!config.IsCaseSensitive)
                {
                    foreach (var key in dict.Keys)
                    {
                        if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                            return TypeHelpers.GuardReflectionLeak(dict[key], $"property {name}");
                    }
                }

                throw new AlderException(DiagnosticDescriptors.MemberNotFound, obj.GetType().Name, name);
            }
        }

        if (obj is NamedTupleValue namedTuple)
        {
            if (namedTuple.TryGetIndex(name, out var idx))
                return namedTuple[idx];
            // Fall through to access fields on the underlying ValueTuple (e.g., Item1, Item2)
            obj = namedTuple.Tuple;
        }

        var type = obj.GetType();

        if (TypedDispatchHelper.TryGetMember(config, type, name, obj, out var typedValue))
            return typedValue;

        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (!config.IsCaseSensitive)
            bindingFlags |= BindingFlags.IgnoreCase;

        var typeMetadata = context.TypeMetadata;
        var prop = typeMetadata.GetProperty(type, name, bindingFlags);
        if (prop != null)
            return TypeHelpers.GuardReflectionLeak(typeMetadata.GetPropertyValue(prop, obj), $"property {name}");

        var field = typeMetadata.GetField(type, name, bindingFlags);
        if (field != null)
            return TypeHelpers.GuardReflectionLeak(field.GetValue(obj), $"field {name}");

        return new MethodRef(obj, name);
    }

    public static object? GetIndex(object? obj, object? index, AlderConfig config, AlderContext context)
    {
        // Unwrap InclusiveRange to raw Range for indexing (inclusive-end only matters for iteration)
        if (index is InclusiveRange inclusive)
            index = inclusive.Value;

        // §12.8.11: System.Index support — resolve from-end indices
        if (index is Index sysIndex && obj != null)
        {
            var length = obj switch
            {
                string s => s.Length,
                Array a => a.Length,
                ICollection c => c.Count,
                _ => -1
            };
            if (length >= 0)
                return GetIndex(obj, (object)sysIndex.GetOffset(length), config, context);
        }

        // §12.8.11: System.Range support — slice arrays/strings
        if (index is Range sysRange && obj != null)
        {
            if (obj is string str)
            {
                var (offset, len) = sysRange.GetOffsetAndLength(str.Length);
                return str.Substring(offset, len);
            }
            if (obj is Array arr)
            {
                var (offset, len) = sysRange.GetOffsetAndLength(arr.Length);
                var elemType = arr.GetType().GetElementType()!;
                var result = Array.CreateInstance(elemType, len);
                Array.Copy(arr, offset, result, 0, len);
                return result;
            }
        }

        switch (obj)
        {
            case null:
                throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);
            case IDictionary<string, object?> dict when index is string strKey:
            {
                if (dict.TryGetValue(strKey, out var value))
                    return TypeHelpers.GuardReflectionLeak(value, $"index [{strKey}]");
                return null;
            }
            case string s when index != null:
            {
                var i = NormalizeIndex(Convert.ToInt32(index), s.Length, config.LanguageMode);
                return (object)s[i];
            }
            case IList list when index != null:
            {
                var idx = NormalizeIndex(Convert.ToInt32(index), list.Count, config.LanguageMode);
                return TypeHelpers.GuardReflectionLeak(list[idx], $"index [{idx}]");
            }
        }

        var type = obj.GetType();

        if (TypedDispatchHelper.TryGetIndex(config, type, obj, index!, out var aotIndexValue))
            return aotIndexValue;

        var indexer = context.TypeMetadata.GetIndexer(type);

        if (indexer != null && indexer.GetIndexParameters().Length == 1)
        {
            try
            {
                var paramType = indexer.GetIndexParameters()[0].ParameterType;
                var safeIndex = ConvertChangeType(index, paramType);
                var val = indexer.GetValue(obj, [safeIndex]);
                TypeHelpers.GuardReflectionLeak(val, "indexer access");
                return val;
            }
            catch (Exception ex) when (ex is not AlderException)
            {
                throw new AlderException(DiagnosticDescriptors.IndexerAccessFailed, ex.Message);
            }
        }

        throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, type.Name);
    }

    public static void SetMember(object? obj, string name, object? value, AlderConfig config, AlderContext context)
    {
        if (obj == null)
            throw new AlderException(DiagnosticDescriptors.NullPropertyAssignment, name);

        var caseInsensitive = !config.IsCaseSensitive;

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

        if (TypedDispatchHelper.TrySetMember(config, type, name, obj, value))
            return;

        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (caseInsensitive)
            bindingFlags |= BindingFlags.IgnoreCase;

        var prop = context.TypeMetadata.GetProperty(type, name, bindingFlags);
        if (prop != null)
        {
            if (!prop.CanWrite)
                throw new AlderException(DiagnosticDescriptors.ReadonlyAssignment);
            prop.SetValue(obj, value);
            return;
        }

        var field = context.TypeMetadata.GetField(type, name, bindingFlags);
        if (field != null)
        {
            if (field.IsInitOnly)
                throw new AlderException(DiagnosticDescriptors.ReadonlyAssignment);
            field.SetValue(obj, value);
            return;
        }

        throw new AlderException(DiagnosticDescriptors.MemberNotFound, type.Name, name);
    }

    public static void SetIndex(object? obj, object? index, object? value, AlderConfig config, AlderContext context)
    {
        if (obj == null)
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);

        if (obj is IDictionary<string, object?> dict && index is string strKey)
        {
            dict[strKey] = value;
            return;
        }

        if (obj is IList list && index != null)
        {
            var idx = NormalizeIndex(Convert.ToInt32(index), list.Count, config.LanguageMode);
            // Coerce value to the array's element type to avoid ArrayTypeMismatchException
            if (obj is Array arr && value != null)
            {
                var elementType = arr.GetType().GetElementType()!;
                if (value.GetType() != elementType && TypeHelpers.CanImplicitlyConvert(value.GetType(), elementType))
                    value = Convert.ChangeType(value, elementType);
            }
            list[idx] = value;
            return;
        }

        var type = obj.GetType();

        if (TypedDispatchHelper.TrySetIndex(config, type, obj, index!, value))
            return;

        var indexer = context.TypeMetadata.GetIndexer(type);

        if (indexer != null && indexer.GetIndexParameters().Length == 1 && indexer.CanWrite)
        {
            try
            {
                var paramType = indexer.GetIndexParameters()[0].ParameterType;
                var safeIndex = ConvertChangeType(index, paramType);
                indexer.SetValue(obj, value, [safeIndex]);
                return;
            }
            catch (Exception ex) when (ex is TargetInvocationException or InvalidCastException or ArgumentException)
            {
                throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, type.Name);
            }
        }

        throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, type.Name);
    }

    public static int NormalizeIndex(int index, int length, LanguageMode languageMode)
    {
        if (index < 0 && languageMode == LanguageMode.Extended)
            index = length + index;

        if (index < 0 || index >= length)
            throw new ArgumentOutOfRangeException(
                "index",
                index,
                "Index was out of range. Must be non-negative and less than the size of the collection.");

        return index;
    }

    /// <summary>
    /// Python-style slice: obj[start:end] where start is inclusive, end is exclusive.
    /// Omitted start defaults to 0, omitted end defaults to length.
    /// Out-of-bounds indices are clamped (Python behavior).
    /// Returns same type as input: T[] -> T[], List&lt;T&gt; -> List&lt;T&gt;, string -> string.
    /// </summary>
    public static object? GetSlice(object? obj, object? start, object? end)
        => GetSlice(obj, start, end, (object?)null);

    /// <summary>
    /// Python-style slice with step: obj[start:end:step].
    /// When step is provided, iterates with the given increment.
    /// Positive step iterates forward, negative step iterates backward.
    /// Step of zero throws an error.
    /// </summary>
    public static object? GetSlice(object? obj, object? start, object? end, object? step)
    {
        if (obj == null)
            throw new AlderException(DiagnosticDescriptors.SliceNull);

        int? stepVal = step != null ? Convert.ToInt32(step) : null;
        if (stepVal == 0)
            throw new AlderException(DiagnosticDescriptors.SliceStepZero);

        int length = obj switch
        {
            string s => s.Length,
            Array arr => arr.Length,
            IList list => list.Count,
            _ => throw new AlderException(DiagnosticDescriptors.SliceUnsupportedType, obj.GetType().Name)
        };

        if (stepVal is null or 1)
        {
            // Fast path: no step or step=1, use original sequential logic
            int startIdx = ResolveIndex(start, stepVal, length, isStart: true);
            int endIdx = ResolveIndex(end, stepVal, length, isStart: false);
            ClampSliceIndices(ref startIdx, ref endIdx, length);
            return SliceSequential(obj, startIdx, endIdx, length);
        }

        // Stepped slice
        return SliceStepped(obj, start, end, stepVal.Value, length);
    }

    private static object? SliceSequential(object obj, int startIdx, int endIdx, int length)
    {
        if (startIdx >= endIdx)
        {
            return obj switch
            {
                string => (object)"",
                Array arr => RuntimeArrayFactory.Create(arr.GetType().GetElementType()!, 0),
                IList list => CreateEmptyResult(list),
                _ => throw new AlderException(DiagnosticDescriptors.SliceUnsupportedType, obj.GetType().Name)
            };
        }

        int count = endIdx - startIdx;
        switch (obj)
        {
            case string s:
                return (object)s.Substring(startIdx, count);
            case Array arr:
            {
                var elementType = arr.GetType().GetElementType()!;
                var result = RuntimeArrayFactory.Create(elementType, count);
                Array.Copy(arr, startIdx, result, 0, count);
                return result;
            }
            case IList list:
                return CollectFromList(list, Enumerable.Range(startIdx, count));
            default:
                throw new AlderException(DiagnosticDescriptors.SliceUnsupportedType, obj.GetType().Name);
        }
    }

    private static object? SliceStepped(object obj, object? start, object? end, int step, int length)
    {
        int startIdx, endIdx;

        if (step > 0)
        {
            startIdx = start != null ? ResolveNegativeIndex(Convert.ToInt32(start), length) : 0;
            endIdx = end != null ? ResolveNegativeIndex(Convert.ToInt32(end), length) : length;
            startIdx = Math.Min(Math.Max(startIdx, 0), length);
            endIdx = Math.Min(Math.Max(endIdx, 0), length);
        }
        else
        {
            // Negative step: default start is last index, default end is "before beginning"
            startIdx = start != null ? ResolveNegativeIndex(Convert.ToInt32(start), length) : length - 1;
            endIdx = end != null ? ResolveNegativeIndex(Convert.ToInt32(end), length) : -1;
            startIdx = Math.Min(Math.Max(startIdx, -1), length - 1);
            // endIdx can be -1 (meaning include index 0)
            endIdx = Math.Min(Math.Max(endIdx, -1), length);
        }

        var indices = new List<int>();
        if (step > 0)
        {
            for (int i = startIdx; i < endIdx; i += step)
                indices.Add(i);
        }
        else
        {
            for (int i = startIdx; i > endIdx; i += step)
                indices.Add(i);
        }

        switch (obj)
        {
            case string s:
            {
                var sb = new System.Text.StringBuilder(indices.Count);
                foreach (var i in indices)
                    sb.Append(s[i]);
                return (object)sb.ToString();
            }
            case Array arr:
            {
                var elementType = arr.GetType().GetElementType()!;
                var result = RuntimeArrayFactory.Create(elementType, indices.Count);
                for (int j = 0; j < indices.Count; j++)
                    result.SetValue(arr.GetValue(indices[j]), j);
                return result;
            }
            case IList list:
                return CollectFromList(list, indices);
            default:
                throw new AlderException(DiagnosticDescriptors.SliceUnsupportedType, obj.GetType().Name);
        }
    }

    private static int ResolveIndex(object? value, int? step, int length, bool isStart)
    {
        if (value != null)
        {
            int idx = Convert.ToInt32(value);
            return idx;
        }
        // Default for null: depends on step direction (but this path is only for step=null or step=1)
        return isStart ? 0 : length;
    }

    private static int ResolveNegativeIndex(int idx, int length)
    {
        if (idx < 0) return length + idx;
        return idx;
    }

    private static object CreateEmptyResult(IList list)
    {
        var listType = list.GetType();
        if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
            return Activator.CreateInstance(listType)!;
        return Array.Empty<object?>();
    }

    private static object CollectFromList(IList list, IEnumerable<int> indices)
    {
        var listType = list.GetType();
        if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var resultList = (IList)Activator.CreateInstance(listType)!;
            foreach (var i in indices)
                resultList.Add(list[i]);
            return resultList;
        }
        var result = new List<object?>();
        foreach (var i in indices)
            result.Add(list[i]);
        return result.ToArray();
    }

    private static void ClampSliceIndices(ref int start, ref int end, int length)
    {
        // Handle negative indices (Python semantics)
        if (start < 0) start = Math.Max(0, length + start);
        if (end < 0) end = Math.Max(0, length + end);

        // Clamp to valid range
        start = Math.Min(Math.Max(start, 0), length);
        end = Math.Min(Math.Max(end, 0), length);
    }

    internal static object? ConvertChangeType(object? value, Type targetType)
    {
        if (value == null) return null;
        if (targetType.IsInstanceOfType(value)) return value;
        return Convert.ChangeType(value, targetType);
    }

}
