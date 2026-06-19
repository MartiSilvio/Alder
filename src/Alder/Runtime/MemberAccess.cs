using System.Diagnostics.CodeAnalysis;
using Alder.Diagnostics;
using Alder.Runtime.Extensions;

namespace Alder.Runtime;

/// <summary>
/// Implements runtime member and index access.
/// This layer is shared by interpreted execution and by compiled paths that defer a member access to runtime.
/// </summary>
internal static class MemberAccess
{
    internal static object? GetResolvedMember(MemberInfo member, object? target, string name, bool nullSafe, AlderContext context)
    {
        if (nullSafe && target == null)
            return null;

        return member switch
        {
            PropertyInfo property => GetResolvedProperty(property, target, name, context),
            FieldInfo field => GetResolvedField(field, target, name, context),
            _ => throw new AlderException(DiagnosticDescriptors.UnsupportedMemberType, member.GetType().Name)
        };
    }

    internal static object? SetResolvedMember(MemberInfo member, object? target, string name, object? value, AlderContext context)
    {
        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "member", name);

        if (!MethodDispatchCache.DynamicCodeSupported)
        {
            if (TypedDispatchHelper.TrySetMember(context.Config, target.GetType(), member.Name, target, value))
                return value;

            throw new AlderException(DiagnosticDescriptors.GeneratedMemberRequired, target.GetType().Name, name);
        }

        if (member is PropertyInfo { CanWrite: true } property)
        {
            property.SetValue(target, value);
            return value;
        }

        if (member is PropertyInfo readonlyProperty)
            throw new AlderException(DiagnosticDescriptors.ReadonlyPropertyAssignment, readonlyProperty.Name);

        if (member is FieldInfo { IsInitOnly: false } field)
        {
            field.SetValue(target, value);
            return value;
        }

        if (member is FieldInfo { IsInitOnly: true })
            throw new AlderException(DiagnosticDescriptors.ReadonlyAssignment);

        throw new AlderException(DiagnosticDescriptors.UnsupportedMemberType, member.GetType().Name);
    }

    public static object? GetMember(object? obj, string name, bool nullSafe, AlderContext context)
    {
        if (nullSafe && obj == null)
            return null;

        if (obj == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, "property", name);

        if (context.Config.LanguageMode == LanguageMode.Extended &&
            DateArithmeticSugar.TryResolveTimeSpanUnit(obj, name, context.Config.IsCaseSensitive, out var timeSpan))
        {
            return timeSpan;
        }

        switch (obj)
        {
            // NamespaceRef carries partially-resolved fully qualified names across chained member access.
            case NamespaceRef nsRef:
            {
                var accumulated = nsRef.Path + "." + name;

                var resolvedType = context.TypeResolver.TryResolveType(accumulated);
                if (resolvedType != null)
                {
                    if (!context.Config.Security.IsTypeAllowed(resolvedType))
                        throw new AlderException(DiagnosticDescriptors.SecurityPolicyTypeBlocked, resolvedType.Name);
                    return resolvedType;
                }

                if (context.TypeResolver.IsNamespaceOrPrefix(accumulated))
                    return new NamespaceRef(accumulated);

                // Once the path is neither a resolvable type nor a namespace prefix, the chain is invalid.
                throw new AlderException(DiagnosticDescriptors.TypeNotFound, accumulated);
            }
            // If typed static dispatch misses, the Type instance itself remains a valid receiver for instance metadata access.
            case Type staticType:
            {
                if (TryGetStaticTypeMember(staticType, name, context, out var staticValue))
                    return staticValue;
                break;
            }
            // Modules are an explicit part of the configured surface, so their members are checked before ordinary security-controlled reflection.
            case ModuleInfo module when module.Members.TryGetValue(name, out var memberEntry):
            {
                if (memberEntry.HasMethods)
                    return new ModuleMethodRef(module, context.ServiceProvider, name);

                // Properties and fields resolve immediately because the value, not the member group, is the expression result.
                var memberInfo = (MemberInfo?)memberEntry.Property ?? memberEntry.Field
                    ?? throw new AlderException(DiagnosticDescriptors.NoMemberOnType, module.Type.Name, name);
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
                return TypeHelpers.GuardReflectionLeak(value, "module member", name);
            }
            case ModuleInfo module:
                throw new AlderException(DiagnosticDescriptors.NoMemberOnType, module.Type.Name, name);
        }

        return GetObjectMember(obj, name, context);
    }

    public static object? GetIndex(object? obj, object? index, AlderContext context)
    {
        // InclusiveRange changes iteration semantics, not CLR indexing semantics.
        if (index is InclusiveRange inclusive)
            index = inclusive.Value;

        // CLR Index and Range support is the runtime endpoint for the corresponding language forms.
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
                return GetIndex(obj, (object)sysIndex.GetOffset(length), context);
        }

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
                var result = RuntimeArrayFactory.Create(elemType, len);
                Array.Copy(arr, offset, result, 0, len);
                return result;
            }
            if (obj is IList list)
            {
                var (offset, len) = sysRange.GetOffsetAndLength(list.Count);
                var listType = list.GetType();
                if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
                {
#if NET7_0_OR_GREATER
                    if (System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported &&
                        RuntimeGenericClosure.DynamicCodeSupported)
#else
                    if (RuntimeGenericClosure.DynamicCodeSupported)
#endif
                    {
                        var resultList = (IList)Activator.CreateInstance(listType)!;
                        for (var i = offset; i < offset + len; i++)
                            resultList.Add(list[i]);
                        return resultList;
                    }
                }
                var items = new object?[len];
                for (var i = 0; i < len; i++)
                    items[i] = list[offset + i];
                return items;
            }
        }

        switch (obj)
        {
            case null:
                throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, TypeNameFormatter.Null);
            case IDictionary<string, object?> dict when index is string strKey:
            {
                if (dict.TryGetValue(strKey, out var value))
                    return TypeHelpers.GuardReflectionLeak(value, "index", strKey);
                return null;
            }
            case string s when index != null:
            {
                var i = NormalizeIndex(Convert.ToInt32(index), s.Length);
                return (object)s[i];
            }
            case IList list when index is int or sbyte or byte or short or ushort or long or ulong:
            {
                var idx = NormalizeIndex(Convert.ToInt32(index), list.Count);
                return TypeHelpers.GuardReflectionLeak(list[idx], "index", idx.ToString());
            }
        }

        var type = obj.GetType();

        if (TypedDispatchHelper.TryGetIndex(context.Config, type, obj, index!, out var aotIndexValue))
            return aotIndexValue;

        if (
#if NET7_0_OR_GREATER
            !System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported ||
#endif
            !MethodDispatchCache.DynamicCodeSupported)
            throw new AlderException(DiagnosticDescriptors.GeneratedMemberRequired, type.Name, "this[]");

        var indexer = FindMatchingIndexer(type, index);

        if (indexer != null)
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
                throw new AlderException(DiagnosticDescriptors.IndexerAccessFailed, ex, ex.Message);
            }
        }

        throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, type.Name);
    }

    public static void SetMember(object? obj, string name, object? value, AlderContext context)
    {
        if (obj == null)
            throw new AlderException(DiagnosticDescriptors.NullPropertyAssignment, name);

        var caseInsensitive = !context.Config.IsCaseSensitive;

        if (obj is StructuralObjectValue structural &&
            structural.TryGetValue(name, context.Config.IsCaseSensitive, out _))
        {
            throw new AlderException(DiagnosticDescriptors.ReadonlyPropertyAssignment, name);
        }

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

        if (TypedDispatchHelper.TrySetMember(context.Config, type, name, obj, value))
            return;

        if (!MethodDispatchCache.DynamicCodeSupported)
            throw new AlderException(DiagnosticDescriptors.GeneratedMemberRequired, type.Name, name);

        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (caseInsensitive)
            bindingFlags |= BindingFlags.IgnoreCase;

        var prop = context.TypeMetadata.GetProperty(type, name, bindingFlags);
        if (prop != null)
        {
            if (!prop.CanWrite)
                throw new AlderException(DiagnosticDescriptors.ReadonlyPropertyAssignment, prop.Name);
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

    public static void SetIndex(object? obj, object? index, object? value, AlderContext context)
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
            var idx = NormalizeIndex(Convert.ToInt32(index), list.Count);
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

        if (TypedDispatchHelper.TrySetIndex(context.Config, type, obj, index!, value))
            return;

        if (
#if NET7_0_OR_GREATER
            !System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported ||
#endif
            !MethodDispatchCache.DynamicCodeSupported)
            throw new AlderException(DiagnosticDescriptors.GeneratedMemberRequired, type.Name, "this[]");

        var indexer = FindMatchingIndexer(type, index);

        if (indexer != null && indexer.CanWrite)
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

    private static object? GetResolvedProperty(PropertyInfo property, object? target, string name, AlderContext context)
    {
        if (property.GetMethod?.IsStatic == true)
        {
            var declaringType = property.DeclaringType ?? property.ReflectedType!;
            return GetResolvedStaticValue(
                context,
                declaringType,
                name,
                property.Name,
                "static property",
                () => property.GetValue(null));
        }

        return GetResolvedInstanceValue(
            context,
            target,
            name,
            property.Name,
            "property",
            "property",
            instance => context.TypeMetadata.GetPropertyValue(property, instance));
    }

    private static object? GetResolvedField(FieldInfo field, object? target, string name, AlderContext context)
    {
        if (field.IsStatic)
        {
            var declaringType = field.DeclaringType ?? field.ReflectedType!;
            return GetResolvedStaticValue(
                context,
                declaringType,
                name,
                field.Name,
                "static field",
                () => field.GetValue(null));
        }

        return GetResolvedInstanceValue(
            context,
            target,
            name,
            field.Name,
            "field",
            "field",
            instance => field.GetValue(instance));
    }

    private static object? GetResolvedStaticValue(
        AlderContext context,
        Type declaringType,
        string requestedName,
        string dispatchName,
        string accessKind,
        Func<object?> readValue)
    {
        if (!MethodDispatchCache.DynamicCodeSupported)
        {
            if (TypedDispatchHelper.TryGetStaticMember(context.Config, declaringType, dispatchName, out var aotValue))
                return aotValue;

            throw new AlderException(DiagnosticDescriptors.GeneratedMemberRequired, declaringType.Name, requestedName);
        }

        return TypeHelpers.GuardReflectionLeak(readValue(), accessKind, requestedName);
    }

    private static object? GetResolvedInstanceValue(
        AlderContext context,
        object? target,
        string requestedName,
        string dispatchName,
        string nullAccessKind,
        string accessKind,
        Func<object, object?> readValue)
    {
        if (target == null)
            throw new AlderException(DiagnosticDescriptors.NullMemberAccess, nullAccessKind, requestedName);

        if (!MethodDispatchCache.DynamicCodeSupported)
        {
            if (TypedDispatchHelper.TryGetMember(context.Config, target.GetType(), dispatchName, target, out var typedValue))
                return typedValue;

            throw new AlderException(DiagnosticDescriptors.GeneratedMemberRequired, target.GetType().Name, requestedName);
        }

        return TypeHelpers.GuardReflectionLeak(readValue(target), accessKind, requestedName);
    }

    private static bool TryGetStaticTypeMember(
        Type staticType,
        string name,
        AlderContext context,
        out object? value)
    {
        if (!context.Config.Security.IsTypeAllowed(staticType))
            throw new AlderException(DiagnosticDescriptors.SecurityPolicyTypeBlocked, staticType.Name);

        if (TypedDispatchHelper.TryGetStaticMember(context.Config, staticType, name, out value))
            return true;

        if (!MethodDispatchCache.DynamicCodeSupported)
            throw new AlderException(DiagnosticDescriptors.GeneratedMemberRequired, staticType.Name, name);

        var staticBindingFlags = BindingFlags.Public | BindingFlags.Static;
        if (!context.Config.IsCaseSensitive)
            staticBindingFlags |= BindingFlags.IgnoreCase;

        var typeMetadata = context.TypeMetadata;
        var staticProp = typeMetadata.GetProperty(staticType, name, staticBindingFlags);
        if (staticProp != null)
        {
            value = TypeHelpers.GuardReflectionLeak(staticProp.GetValue(null), "static property", name);
            return true;
        }

        var staticField = typeMetadata.GetField(staticType, name, staticBindingFlags);
        if (staticField != null)
        {
            value = TypeHelpers.GuardReflectionLeak(staticField.GetValue(null), "static field", name);
            return true;
        }

        var staticMethods = typeMetadata.GetMethods(staticType, name, staticBindingFlags);
        if (staticMethods.Length > 0)
        {
            value = new MethodRef(staticType, name);
            return true;
        }

        value = null;
        return false;
    }

    private static object? GetObjectMember(object obj, string name, AlderContext context)
    {
        if (obj is StructuralObjectValue structural &&
            structural.TryGetValue(name, context.Config.IsCaseSensitive, out var structuralValue))
        {
            return TypeHelpers.GuardReflectionLeak(structuralValue, "property", name);
        }

        if (obj is IDictionary<string, object?> dict)
            return GetDictionaryMember(dict, obj.GetType().Name, name, context.Config.IsCaseSensitive);

        if (obj is NamedTupleValue namedTuple)
        {
            if (namedTuple.TryGetIndex(name, out var idx))
                return namedTuple[idx];
            obj = namedTuple.Tuple;
        }

        var type = obj.GetType();

        if (TypeHelpers.IsValueTupleType(type) && TryAccessLargeTupleItem(obj, name, out var tupleItem))
            return tupleItem;

        if (TypedDispatchHelper.TryGetMember(context.Config, type, name, obj, out var typedValue))
            return typedValue;

        if (!MethodDispatchCache.DynamicCodeSupported)
            throw new AlderException(DiagnosticDescriptors.GeneratedMemberRequired, type.Name, name);

        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (!context.Config.IsCaseSensitive)
            bindingFlags |= BindingFlags.IgnoreCase;

        var typeMetadata = context.TypeMetadata;
        var prop = typeMetadata.GetProperty(type, name, bindingFlags);
        if (prop != null)
            return TypeHelpers.GuardReflectionLeak(typeMetadata.GetPropertyValue(prop, obj), "property", name);

        var field = typeMetadata.GetField(type, name, bindingFlags);
        if (field != null)
            return TypeHelpers.GuardReflectionLeak(field.GetValue(obj), "field", name);

        return new MethodRef(obj, name);
    }

    private static object? GetDictionaryMember(
        IDictionary<string, object?> dict,
        string typeName,
        string name,
        bool isCaseSensitive)
    {
        if (dict.TryGetValue(name, out var value))
            return TypeHelpers.GuardReflectionLeak(value, "property", name);

        if (!isCaseSensitive)
        {
            foreach (var key in dict.Keys)
            {
                if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    return TypeHelpers.GuardReflectionLeak(dict[key], "property", name);
            }
        }

        throw new AlderException(DiagnosticDescriptors.MemberNotFound, typeName, name);
    }

    private static bool TryAccessLargeTupleItem(object tuple, string name, out object? value)
    {
        value = null;
        if (name.Length < 5 || !name.StartsWith("Item", StringComparison.Ordinal))
            return false;
        if (!int.TryParse(name.Substring(4), out var itemIndex) || itemIndex < 8)
            return false;

#if NETSTANDARD2_0
        var current = tuple;
        while (itemIndex > 7)
        {
            var restField = current.GetType().GetField("Rest");
            if (restField == null) return false;
            current = restField.GetValue(current)!;
            itemIndex -= 7;
        }

        var field = current.GetType().GetField($"Item{itemIndex}");
        if (field == null) return false;
        value = field.GetValue(current);
#else
        if (tuple is not System.Runtime.CompilerServices.ITuple tupleValue ||
            itemIndex > tupleValue.Length)
            return false;

        value = tupleValue[itemIndex - 1];
#endif
        return true;
    }

    private static PropertyInfo? FindMatchingIndexer(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type, object? index)
    {
        var indexType = index?.GetType();

        foreach (var property in RuntimeTypeIntrospection.GetProperties(type, BindingFlags.Public | BindingFlags.Instance))
        {
            var parameters = property.GetIndexParameters();
            if (parameters.Length == 1 &&
                (indexType == null || parameters[0].ParameterType.IsAssignableFrom(indexType)))
                return property;
        }

        // §12.8.11.3: if an indexer with the right arity exists but the argument type doesn't
        // match, Roslyn reports CS1503 rather than CS0021. Detect that case and throw the more
        // precise diagnostic so the argument-type-mismatch path surfaces to the caller.
        if (indexType != null)
        {
            foreach (var property in RuntimeTypeIntrospection.GetProperties(type, BindingFlags.Public | BindingFlags.Instance))
            {
                var parameters = property.GetIndexParameters();
                if (parameters.Length == 1)
                {
                    throw new AlderException(
                        DiagnosticDescriptors.ArgumentConversionFailed,
                        "1", indexType.Name, parameters[0].ParameterType.Name);
                }
            }
        }

        return null;
    }

    public static int NormalizeIndex(int index, int length)
    {
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
                var sb = new StringBuilder(indices.Count);
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
        {
#if NET7_0_OR_GREATER
            if (System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported &&
                RuntimeGenericClosure.DynamicCodeSupported)
#else
            if (RuntimeGenericClosure.DynamicCodeSupported)
#endif
                return Activator.CreateInstance(listType)!;
        }
        return Array.Empty<object?>();
    }

    private static object CollectFromList(IList list, IEnumerable<int> indices)
    {
        var listType = list.GetType();
        if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
        {
#if NET7_0_OR_GREATER
            if (System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported &&
                RuntimeGenericClosure.DynamicCodeSupported)
#else
            if (RuntimeGenericClosure.DynamicCodeSupported)
#endif
            {
                var resultList = (IList)Activator.CreateInstance(listType)!;
                foreach (var i in indices)
                    resultList.Add(list[i]);
                return resultList;
            }
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
