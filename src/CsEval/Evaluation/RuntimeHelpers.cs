using System.Linq;
using System.Reflection;

namespace CsEval.Evaluation;

/// <summary>
/// Static helper methods called by both compiled (IL) and interpreted (AST) expressions at runtime.
/// Centralizes operator logic to ensure consistent behavior between execution modes.
/// </summary>
public static class RuntimeHelpers
{
    public static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            int i => i != 0,
            long l => l != 0,
            double d => d != 0,
            string s => !string.IsNullOrEmpty(s),
            _ => true
        };
    }

    public static object? Negate(object? value)
    {
        if (IsNumeric(value))
            return -(dynamic)value!;

        throw new CsEvalException($"Cannot negate {value?.GetType().Name ?? "null"}");
    }

    public static object? Add(object? left, object? right, CsEvalOptions options) =>
        Add(left, right, options, null);

    public static object? Add(object? left, object? right, CsEvalOptions options, CsEvalContext? context)
    {
        if (left is string || right is string)
            return $"{left}{right}";

        if (IsNumeric(left) && IsNumeric(right))
            return (dynamic)left! + (dynamic)right!;

        return MergeObjects(left, right, options, context);
    }

    private static object? MergeObjects(object? left, object? right, CsEvalOptions options, CsEvalContext? context)
    {
        var comparer = options.StringComparer;
        var merged = new Dictionary<string, object?>(comparer);

        CopyObjectProperties(left, merged, context);
        CopyObjectProperties(right, merged, context);

        if (merged.Count == 0 && (left != null || right != null))
            throw new CsEvalException($"Cannot add {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");

        return merged;
    }

    private static void CopyObjectProperties(object? obj, Dictionary<string, object?> target, CsEvalContext? context)
    {
        if (obj == null) return;

        if (obj is IDictionary<string, object?> dict)
        {
            foreach (var kvp in dict)
                target[kvp.Key] = kvp.Value;
            return;
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;

        if (context != null)
        {
            foreach (var prop in context.TypeCache.GetProperties(type, bindingFlags))
            {
                if (prop.CanRead)
                    target[prop.Name] = context.TypeCache.GetPropertyValue(prop, obj);
            }
        }
        else
        {
            foreach (var prop in type.GetProperties(bindingFlags))
            {
                if (prop.CanRead)
                    target[prop.Name] = prop.GetValue(obj);
            }
        }
    }

    public static object? Subtract(object? left, object? right, CsEvalOptions options)
    {
        if (IsNumeric(left) && IsNumeric(right))
            return (dynamic)left! - (dynamic)right!;

        throw new CsEvalException($"Cannot subtract {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object? Multiply(object? left, object? right, CsEvalOptions options)
    {
        if (IsNumeric(left) && IsNumeric(right))
            return (dynamic)left! * (dynamic)right!;

        throw new CsEvalException($"Cannot multiply {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object? Divide(object? left, object? right, CsEvalOptions options)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            // Only throw DivideByZeroException for integers; floats return Infinity
            if ((dynamic)right! == 0 && IsInteger(left) && IsInteger(right))
                throw new DivideByZeroException();
            return (dynamic)left! / (dynamic)right!;
        }

        throw new CsEvalException($"Cannot divide {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object? Modulo(object? left, object? right, CsEvalOptions options)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            // Only throw DivideByZeroException for integers; floats return NaN
            if ((dynamic)right! == 0 && IsInteger(left) && IsInteger(right))
                throw new DivideByZeroException();
            return (dynamic)left! % (dynamic)right!;
        }

        throw new CsEvalException($"Cannot modulo {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object Equals(object? left, object? right, CsEvalOptions options)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        if (left.Equals(right)) return true;

        // Let C# runtime handle numeric comparison via dynamic
        if (IsNumeric(left) && IsNumeric(right))
        {
            // C# forbids decimal == float/double, so handle that case specially
            if (InvolvesDecimalAndFloatingPoint(left, right))
            {
                return Convert.ToDouble(left) == Convert.ToDouble(right);
            }
            return (dynamic)left! == (dynamic)right!;
        }

        return false;
    }

    private static bool InvolvesDecimalAndFloatingPoint(object? a, object? b)
    {
        var aIsDecimal = a is decimal;
        var bIsDecimal = b is decimal;
        var aIsFloatingPoint = a is float or double;
        var bIsFloatingPoint = b is float or double;
        return (aIsDecimal && bIsFloatingPoint) || (bIsDecimal && aIsFloatingPoint);
    }

    public static object NotEquals(object? left, object? right, CsEvalOptions options)
    {
        return !(bool)Equals(left, right, options);
    }

    public static object LessThan(object? left, object? right, CsEvalOptions options)
    {
        return Compare(left, right, options) < 0;
    }

    public static object LessThanOrEqual(object? left, object? right, CsEvalOptions options)
    {
        return Compare(left, right, options) <= 0;
    }

    public static object GreaterThan(object? left, object? right, CsEvalOptions options)
    {
        return Compare(left, right, options) > 0;
    }

    public static object GreaterThanOrEqual(object? left, object? right, CsEvalOptions options)
    {
        return Compare(left, right, options) >= 0;
    }

    internal static int Compare(object? left, object? right, CsEvalOptions options)
    {
        if (left == null || right == null)
            throw new CsEvalException("Cannot compare null values");

        // Let C# runtime handle comparison via dynamic
        if (IsNumeric(left) && IsNumeric(right))
        {
            dynamic l = left, r = right;
            return l < r ? -1 : l > r ? 1 : 0;
        }

        return left switch
        {
            string ls when right is string rs => string.Compare(ls, rs, options.StringComparison),
            IComparable comparable => comparable.CompareTo(right),
            _ => throw new CsEvalException($"Cannot compare {left.GetType().Name} and {right.GetType().Name}")
        };
    }

    public static object? GetMember(object? obj, string name, CsEvalOptions options, bool nullSafe, CsEvalContext context)
    {
        if (nullSafe && obj == null)
            return null;

        if (obj == null)
            throw new CsEvalException($"Cannot access property '{name}' on null");

        if (!options.Sandbox.AllowPropertyRead)
            throw new CsEvalException($"Property access blocked by sandbox: {name}");

        // Handle module resolver - only allow access to members in the Members dictionary
        if (obj is CsEvalEngine.ModuleResolver resolver)
        {
            if (resolver.Members.TryGetValue(name, out var memberInfo))
            {
                var instance = resolver.Resolve();
                var value = memberInfo switch
                {
                    PropertyInfo p => p.GetValue(p.GetMethod!.IsStatic ? null : instance),
                    FieldInfo f => f.GetValue(f.IsStatic ? null : instance),
                    _ => throw new CsEvalException($"Member '{name}' is not a property or field")
                };
                return CheckSandboxType(value, options.Sandbox);
            }
            // Member not in dictionary - not allowed (matches tree-walking evaluator behavior)
            throw new CsEvalException($"Member '{name}' not found on module '{resolver.Type.Name}'");
        }

        if (obj is IDictionary<string, object?> dict)
        {
            if (dict.TryGetValue(name, out var value))
                return CheckSandboxType(value, options.Sandbox);

            if (options.IgnoreCase)
            {
                foreach (var key in dict.Keys)
                {
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        return CheckSandboxType(dict[key], options.Sandbox);
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
            return CheckSandboxType(typeCache.GetPropertyValue(prop, obj), options.Sandbox);

        var field = typeCache.GetField(type, name, bindingFlags);
        if (field != null)
            return CheckSandboxType(field.GetValue(obj), options.Sandbox);

        throw new CsEvalException($"Property '{name}' not found on type '{type.Name}'");
    }

    /// <summary>
    /// Checks if a type is a forbidden reflection metadata type.
    /// </summary>
    internal static bool IsForbiddenReflectionType(Type? type)
    {
        if (type == null) return false;

        // Block System.Type (includes RuntimeType)
        if (typeof(Type).IsAssignableFrom(type))
            return true;

        // Block all reflection metadata types (MemberInfo is base for MethodInfo, PropertyInfo, FieldInfo, etc.)
        if (typeof(MemberInfo).IsAssignableFrom(type))
            return true;

        // Block Assembly and Module
        if (typeof(Assembly).IsAssignableFrom(type))
            return true;
        if (typeof(Module).IsAssignableFrom(type))
            return true;

        // Block runtime handles
        if (type == typeof(RuntimeTypeHandle) ||
            type == typeof(RuntimeMethodHandle) ||
            type == typeof(RuntimeFieldHandle))
            return true;

        // Block MethodBody (not a MemberInfo subclass)
        if (typeof(MethodBody).IsAssignableFrom(type))
            return true;

        // Block Reflection.Emit types by namespace check
        if (type.Namespace is "System.Reflection.Emit")
            return true;

        // Block pointer types
        if (type.IsPointer || type == typeof(IntPtr) || type == typeof(UIntPtr))
            return true;

        // Check if it's an array/collection of forbidden types
        if (type.IsArray && IsForbiddenReflectionType(type.GetElementType()))
            return true;

        // Check generic type arguments (e.g., List<Type> should be blocked)
        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                if (IsForbiddenReflectionType(arg))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Guards against reflection type leaks. Throws if value is a forbidden reflection type.
    /// </summary>
    public static object? CheckSandboxType(object? value, SandboxOptions options)
    {
        if (value == null) return null;

        var type = value.GetType();
        if (IsForbiddenReflectionType(type))
        {
            throw new CsEvalException($"Access to reflection types is not allowed: {type.Name}");
        }

        return value;
    }

    public static object? BitwiseAnd(object? left, object? right, CsEvalOptions options)
    {
        if (IsInteger(left) && IsInteger(right))
            return (dynamic)left! & (dynamic)right!;
        
        if (left is bool lb && right is bool rb)
            return lb & rb;

        throw new CsEvalException($"Cannot apply operator & to {left?.GetType().Name} and {right?.GetType().Name}");
    }

    public static object? BitwiseOr(object? left, object? right, CsEvalOptions options)
    {
        if (IsInteger(left) && IsInteger(right))
            return (dynamic)left! | (dynamic)right!;

        if (left is bool lb && right is bool rb)
            return lb | rb;

        throw new CsEvalException($"Cannot apply operator | to {left?.GetType().Name} and {right?.GetType().Name}");
    }

    public static object? BitwiseXor(object? left, object? right, CsEvalOptions options)
    {
        if (IsInteger(left) && IsInteger(right))
            return (dynamic)left! ^ (dynamic)right!;

        if (left is bool lb && right is bool rb)
            return lb ^ rb;

        throw new CsEvalException($"Cannot apply operator ^ to {left?.GetType().Name} and {right?.GetType().Name}");
    }

    public static object? BitwiseNot(object? value)
    {
        if (!IsNumeric(value))
            throw new CsEvalException($"Cannot apply bitwise NOT to {value?.GetType().Name ?? "null"}");

        return ~(dynamic)value!;
    }

    public static object? LeftShift(object? left, object? right)
    {
        if (!IsNumeric(left) || !IsNumeric(right))
            throw new CsEvalException($"Cannot apply left shift to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");

        return (dynamic)left! << (int)(dynamic)right!;
    }

    public static object? RightShift(object? left, object? right)
    {
        if (!IsNumeric(left) || !IsNumeric(right))
            throw new CsEvalException($"Cannot apply right shift to {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");

        return (dynamic)left! >> (int)(dynamic)right!;
    }

    public static bool Contains(object? collection, object? value, CsEvalOptions options)
    {
        if (collection == null)
            throw new CsEvalException("Cannot check containment in null collection");

        // String containment: "bc" in "abcd"
        if (collection is string str && value is string substr)
            return str.Contains(substr);

        // Character in string: 'b' in "abc"
        if (collection is string strForChar && value is char ch)
            return strForChar.Contains(ch);

        // Collection containment
        if (collection is System.Collections.IEnumerable enumerable)
            return enumerable.Cast<object?>().Any(item => (bool)Equals(item, value, options));

        throw new CsEvalException($"Cannot use 'in' operator with {collection.GetType().Name}");
    }

    internal static bool IsInteger(object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong;

    internal static bool IsNumeric(object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

    public static object? GetIndex(object? obj, object? index, CsEvalOptions options)
    {
        if (obj == null)
            throw new CsEvalException("Cannot index null");

        if (obj is IDictionary<string, object?> dict)
        {
            var key = index?.ToString() ?? "";
            var val = dict.TryGetValue(key, out var v) ? v : null;
            CheckSandboxType(val, options.Sandbox);
            return val;
        }

        if (obj is System.Collections.IList list)
        {
            if (index is int i)
            {
                if (i < 0 || i >= list.Count) throw new CsEvalException($"Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')");
                var val = list[i];
                CheckSandboxType(val, options.Sandbox);
                return val;
            }
            throw new CsEvalException($"Hashtable/List index must be an integer, got {index?.GetType().Name}");
        }
        
        // Handle standard arrays and other indexers via reflection
        var type = obj.GetType();
        var indexer = type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
        
        if (indexer != null && indexer.GetIndexParameters().Length == 1)
        {
             try 
             {
                 // Try to convert index to expected type
                 var paramType = indexer.GetIndexParameters()[0].ParameterType;
                 var safeIndex = ConvertChangeType(index, paramType);
                 var val = indexer.GetValue(obj, new[] { safeIndex });
                 CheckSandboxType(val, options.Sandbox);
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
                
                // Convert value to match list element type if possible
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
                 
                 // We might need to convert value too depending on setter type
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

    private static object? ConvertChangeType(object? value, Type targetType)
    {
        if (value == null) return null;
        if (targetType.IsInstanceOfType(value)) return value;
        return Convert.ChangeType(value, targetType);
    }

    public static void CheckAllowAssignment(CsEvalOptions options, string context)
    {
        if (!options.Sandbox.AllowAssignment)
            throw new CsEvalException($"Assignment blocked by sandbox: {context}");
    }

    public static void CheckIterationLimit(long iterations, CsEvalOptions options)
    {
        if (options.MaxIterations > 0 && iterations > options.MaxIterations)
            throw new CsEvalException($"Loop exceeded maximum iterations ({options.MaxIterations}). Possible infinite loop.");
    }

    /// <summary>
    /// Maps type name strings to their corresponding CLR types.
    /// </summary>
    private static readonly Dictionary<string, Type> TypeNameToClrType = new()
    {
        ["sbyte"] = typeof(sbyte),
        ["byte"] = typeof(byte),
        ["short"] = typeof(short),
        ["ushort"] = typeof(ushort),
        ["int"] = typeof(int),
        ["uint"] = typeof(uint),
        ["long"] = typeof(long),
        ["ulong"] = typeof(ulong),
        ["float"] = typeof(float),
        ["double"] = typeof(double),
        ["decimal"] = typeof(decimal),
        ["bool"] = typeof(bool),
        ["char"] = typeof(char),
        ["string"] = typeof(string),
        ["object"] = typeof(object),
        // Nullable types
        ["sbyte?"] = typeof(sbyte?),
        ["byte?"] = typeof(byte?),
        ["short?"] = typeof(short?),
        ["ushort?"] = typeof(ushort?),
        ["int?"] = typeof(int?),
        ["uint?"] = typeof(uint?),
        ["long?"] = typeof(long?),
        ["ulong?"] = typeof(ulong?),
        ["float?"] = typeof(float?),
        ["double?"] = typeof(double?),
        ["decimal?"] = typeof(decimal?),
        ["bool?"] = typeof(bool?),
        ["char?"] = typeof(char?),
    };

    /// <summary>
    /// C# implicit numeric conversions table.
    /// Key: source type, Value: set of types it can implicitly convert to.
    /// Based on ECMA-334 (C# Language Specification).
    /// </summary>
    private static readonly Dictionary<Type, HashSet<Type>> ImplicitConversions = new()
    {
        [typeof(sbyte)] = [typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(byte)] = [typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(short)] = [typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(ushort)] = [typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(int)] = [typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(uint)] = [typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(long)] = [typeof(float), typeof(double), typeof(decimal)],
        [typeof(ulong)] = [typeof(float), typeof(double), typeof(decimal)],
        [typeof(float)] = [typeof(double)],
        [typeof(char)] = [typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
    };

    /// <summary>
    /// Validates and coerces the value to the declared type for variable declarations.
    /// </summary>
    public static object? ValidateAndCoerceType(string typeName, object? value, string varName)
    {
        if (typeName == "object")
            return value;

        if (!TypeNameToClrType.TryGetValue(typeName, out var targetType))
            throw new CsEvalException($"Unknown type '{typeName}'");

        var isNullable = Nullable.GetUnderlyingType(targetType) != null;
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value == null)
        {
            if (targetType.IsValueType && !isNullable)
                throw new CsEvalException($"Cannot assign null to {typeName} variable '{varName}'");
            return null;
        }

        var sourceType = value.GetType();

        if (sourceType == underlyingType || sourceType == targetType)
            return value;

        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(underlyingType))
            return Convert.ChangeType(value, underlyingType);

        if (underlyingType == typeof(char) && value is string { Length: 1 } s)
            return s[0];

        throw new CsEvalException($"Cannot assign {sourceType.Name} to {typeName} variable '{varName}'");
    }

    public static System.Collections.IEnumerator GetEnumerator(object? collection)
    {
        if (collection is not System.Collections.IEnumerable enumerable)
            throw new CsEvalException($"Cannot iterate over type '{collection?.GetType().Name ?? "null"}' in foreach");

        return enumerable.GetEnumerator();
    }
}
