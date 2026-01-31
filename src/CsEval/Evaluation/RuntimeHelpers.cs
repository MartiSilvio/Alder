using System.Linq;
using System.Reflection;

namespace CsEval.Evaluation;

/// <summary>
/// Static helper methods called by both compiled (IL) and interpreted (AST) expressions at runtime.
/// Centralizes operator logic to ensure consistent behavior between execution modes.
/// </summary>
public static class RuntimeHelpers
{
    public static bool RequireBoolean(object? value)
    {
        if (value is bool b)
            return b;

        throw new CsEvalException($"Condition must evaluate to a boolean, got '{value?.GetType().Name ?? "null"}'");
    }

    public static object? Negate(object? value)
    {
        if (IsNumeric(value))
            return -(dynamic)value!;

        throw new CsEvalException($"Cannot negate {value?.GetType().Name ?? "null"}");
    }

    /// <summary>
    /// Resolves an identifier by first checking the functions dictionary, then the context.
    /// </summary>
    public static object? ResolveIdentifier(string name, CsEvalContext context, Dictionary<string, Func<object?[], object?>> functions)
    {
        if (functions.ContainsKey(name))
            return new FunctionRef(name, functions[name]);

        return context.Get(name);
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
                    MethodInfo m => new ModuleMethodRef(resolver, m),
                    PropertyInfo p => p.GetValue(p.GetMethod!.IsStatic ? null : instance),
                    FieldInfo f => f.GetValue(f.IsStatic ? null : instance),
                    _ => throw new CsEvalException($"Unsupported member type '{memberInfo.GetType().Name}'")
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

    /// <summary>
    /// Invokes a method call on a target object. Handles instance methods and LINQ extension methods.
    /// Used by IL-compiled code for member access calls (target.Method(args)).
    /// </summary>
    public static object? InvokeMemberCall(
        object? target,
        string methodName,
        object?[] args,
        bool nullSafe,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        Dictionary<string, Func<object?[], object?>> functions,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer)
    {
        // Handle null-safe member access
        if (nullSafe && target == null)
            return null;

        if (target == null)
            throw new CsEvalException($"Cannot call method '{methodName}' on null");

        // Try instance method first (includes LINQ extension methods)
        var result = TryInvokeInstanceMethod(target, methodName, args, context, options, ct, argumentTransformer);
        if (result.Success)
            return result.Value;

        // Fall back to getting member (for ModuleResolver methods)
        var callee = GetMember(target, methodName, options, nullSafe, context);
        return InvokeCall(callee, args, functions, context, options, ct, argumentTransformer);
    }

    /// <summary>
    /// Invokes a call expression at runtime. Used by IL-compiled code.
    /// </summary>
    public static object? InvokeCall(
        object? callee,
        object?[] args,
        Dictionary<string, Func<object?[], object?>> functions,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer)
    {
        if (callee is MethodRef methodRef)
        {
            var result = TryInvokeInstanceMethod(methodRef.Target, methodRef.MethodName, args, context, options, ct, argumentTransformer);
            if (result.Success)
                return result.Value;
            throw new CsEvalException($"Method '{methodRef.MethodName}' invocation failed");
        }

        if (callee is ModuleMethodRef moduleRef)
        {
            return InvokeModuleMethod(moduleRef, args, context, ct, argumentTransformer);
        }

        if (callee is FunctionRef funcRef)
            return funcRef.Invoke(args);

        if (callee is Delegate del)
            return del.DynamicInvoke(args);

        if (callee is LambdaValue lambda)
            return InvokeLambda(lambda, args, context);

        throw new CsEvalException($"Cannot call '{callee?.GetType().Name ?? "null"}' as a function");
    }

    /// <summary>
    /// Attempts to invoke an instance method. Used by IL-compiled code for member access calls.
    /// </summary>
    public static (bool Success, object? Value) TryInvokeInstanceMethod(
        object? target,
        string methodName,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer)
    {
        if (target == null)
            return (false, null);

        if (target is CsEvalEngine.ModuleResolver)
            return (false, null);

        var type = target.GetType();

        // LINQ methods are always allowed
        if (target is System.Collections.IEnumerable enumerable && !type.IsPrimitive && target is not string)
        {
            var result = TryInvokeEnumerableMethod(enumerable, methodName, args, context, options);
            if (result.Success)
                return result;
        }

        // Sandbox blocks method calls on variable objects when BlockMethodCalls is true
        if (options.Sandbox.BlockMethodCalls)
            throw new CsEvalException($"Method calls blocked by sandbox: {methodName}");

        var methods = context.TypeCache.GetMethods(type, methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var argsWithCancellation = TryAppendCancellationToken(parameters, args, ct);
            if (CanInvokeMethod(parameters, argsWithCancellation, out var convertedArgs))
            {
                if (argumentTransformer != null)
                    convertedArgs = argumentTransformer(method, convertedArgs);

                var result = method.Invoke(target, convertedArgs);
                return (true, GuardReflectionLeak(result, $"method {methodName}"));
            }
        }

        return (false, null);
    }

    private static object? InvokeModuleMethod(
        ModuleMethodRef methodRef,
        object?[] args,
        CsEvalContext context,
        CancellationToken ct,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer)
    {
        var methodName = methodRef.Method.Name;
        var resolver = methodRef.Resolver;
        var target = methodRef.Method.IsStatic ? null : resolver.Resolve();

        var methods = context.TypeCache.GetMethods(resolver.Type, methodName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        foreach (var method in methods)
        {
            if (method.ContainsGenericParameters)
            {
                var concreteMethod = TryMakeConcreteMethod(method, args);
                if (concreteMethod != null)
                {
                    var result = InvokeMethodWithArgs(concreteMethod, target, args, ct, argumentTransformer);
                    if (result.Success)
                        return result.Value;
                }
                continue;
            }

            var invokeResult = InvokeMethodWithArgs(method, target, args, ct, argumentTransformer);
            if (invokeResult.Success)
                return invokeResult.Value;
        }

        // Fallback to original method
        var fallbackMethod = methodRef.Method;
        var fallbackParams = fallbackMethod.GetParameters();
        var finalArgs = TryAppendCancellationToken(fallbackParams, args, ct);

        if (argumentTransformer != null)
            finalArgs = argumentTransformer(fallbackMethod, finalArgs);

        finalArgs = PadWithDefaults(fallbackParams, finalArgs);

        var fallbackResult = fallbackMethod.Invoke(target, finalArgs);
        return GuardReflectionLeak(fallbackResult, $"method {methodName}");
    }

    private static (bool Success, object? Value) InvokeMethodWithArgs(
        MethodInfo method,
        object? target,
        object?[] args,
        CancellationToken ct,
        Func<MethodInfo, object?[], object?[]>? argumentTransformer)
    {
        var parameters = method.GetParameters();
        var argsWithCancellation = TryAppendCancellationToken(parameters, args, ct);

        if (CanInvokeMethod(parameters, argsWithCancellation, out var convertedArgs))
        {
            if (argumentTransformer != null)
                convertedArgs = argumentTransformer(method, convertedArgs);

            var result = method.Invoke(target, convertedArgs);
            return (true, GuardReflectionLeak(result, $"method {method.Name}"));
        }

        return (false, null);
    }

    private static MethodInfo? TryMakeConcreteMethod(MethodInfo genericMethod, object?[] args)
    {
        var genericArgs = genericMethod.GetGenericArguments();

        if (genericArgs.Length != 1 || args.Length == 0)
            return null;

        var firstArg = args[0];
        if (firstArg == null)
            return null;

        try
        {
            return genericMethod.MakeGenericMethod(firstArg.GetType());
        }
        catch
        {
            return null;
        }
    }

    private static object?[] TryAppendCancellationToken(ParameterInfo[] parameters, object?[] args, CancellationToken ct)
    {
        if (parameters.Length == 0)
            return args;

        var lastParam = parameters[^1];
        if (lastParam.ParameterType == typeof(CancellationToken) && args.Length == parameters.Length - 1)
        {
            var newArgs = new object?[args.Length + 1];
            Array.Copy(args, newArgs, args.Length);
            newArgs[^1] = ct;
            return newArgs;
        }

        return args;
    }

    private static bool CanInvokeMethod(ParameterInfo[] parameters, object?[] args, out object?[] convertedArgs)
    {
        convertedArgs = new object?[parameters.Length];

        var positionalArgs = new List<object?>();
        var namedArgs = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var arg in args)
        {
            if (arg is NamedArg named)
                namedArgs[named.Name] = named.Value;
            else
                positionalArgs.Add(arg);
        }

        var filledParams = new bool[parameters.Length];
        var positionalIndex = 0;

        for (var i = 0; i < parameters.Length && positionalIndex < positionalArgs.Count; i++)
        {
            if (namedArgs.ContainsKey(parameters[i].Name!))
                continue;

            var arg = positionalArgs[positionalIndex++];
            if (!TryConvertArg(arg, parameters[i].ParameterType, out var converted))
                return false;

            convertedArgs[i] = converted;
            filledParams[i] = true;
        }

        if (positionalIndex < positionalArgs.Count)
            return false;

        foreach (var (name, value) in namedArgs)
        {
            var paramIndex = -1;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (string.Equals(parameters[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    paramIndex = i;
                    break;
                }
            }

            if (paramIndex == -1)
                return false;

            if (!TryConvertArg(value, parameters[paramIndex].ParameterType, out var converted))
                return false;

            convertedArgs[paramIndex] = converted;
            filledParams[paramIndex] = true;
        }

        for (var i = 0; i < parameters.Length; i++)
        {
            if (filledParams[i])
                continue;

            if (parameters[i].HasDefaultValue)
                convertedArgs[i] = parameters[i].DefaultValue;
            else
                return false;
        }

        return true;
    }

    private static bool TryConvertArg(object? arg, Type targetType, out object? converted)
    {
        converted = null;

        if (arg == null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                return false;
            return true;
        }

        if (targetType.IsAssignableFrom(arg.GetType()))
        {
            converted = arg;
            return true;
        }

        try
        {
            converted = Convert.ChangeType(arg, targetType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static object?[] PadWithDefaults(ParameterInfo[] parameters, object?[] args)
    {
        if (parameters.Length == 0)
            return [];

        var lastParam = parameters[^1];
        var isParams = lastParam.IsDefined(typeof(ParamArrayAttribute), false);

        if (isParams)
            return PadWithParamsArray(parameters, args, lastParam);

        var result = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            if (i < args.Length)
                result[i] = CoerceNumeric(args[i], parameters[i].ParameterType);
            else if (parameters[i].HasDefaultValue)
                result[i] = parameters[i].DefaultValue;
            else
                throw new CsEvalException($"Missing required argument '{parameters[i].Name}'");
        }

        return result;
    }

    private static object?[] PadWithParamsArray(ParameterInfo[] parameters, object?[] args, ParameterInfo paramsParam)
    {
        var normalParamCount = parameters.Length - 1;
        var result = new object?[parameters.Length];

        for (var i = 0; i < normalParamCount; i++)
        {
            if (i < args.Length)
                result[i] = CoerceNumeric(args[i], parameters[i].ParameterType);
            else if (parameters[i].HasDefaultValue)
                result[i] = parameters[i].DefaultValue;
            else
                throw new CsEvalException($"Missing required argument '{parameters[i].Name}'");
        }

        var paramsElementType = paramsParam.ParameterType.GetElementType()!;
        var paramsCount = Math.Max(0, args.Length - normalParamCount);
        var paramsArray = Array.CreateInstance(paramsElementType, paramsCount);

        for (var i = 0; i < paramsCount; i++)
        {
            var value = CoerceNumeric(args[normalParamCount + i], paramsElementType);
            paramsArray.SetValue(value, i);
        }

        result[normalParamCount] = paramsArray;
        return result;
    }

    private static object? CoerceNumeric(object? arg, Type targetType)
    {
        if (arg == null) return null;
        if (targetType.IsInstanceOfType(arg)) return arg;

        if (arg is IConvertible)
        {
            try
            {
                var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
                return Convert.ChangeType(arg, underlying);
            }
            catch
            {
                return arg;
            }
        }

        return arg;
    }

    private static object? GuardReflectionLeak(object? value, string context)
    {
        if (value == null) return null;

        var type = value.GetType();
        if (IsForbiddenReflectionType(type))
            throw new CsEvalException($"Access to reflection types is not allowed: {type.Name} ({context})");

        return value;
    }

    private static object? InvokeLambda(LambdaValue lambda, object?[] args, CsEvalContext context)
    {
        var childContext = lambda.Closure.CreateChild();
        for (var i = 0; i < lambda.Parameters.Count && i < args.Length; i++)
        {
            childContext.Define(lambda.Parameters[i], args[i]);
        }

        // Lambda invocation requires tree-walking evaluation
        // This is a limitation: IL-compiled code calling lambdas falls back to interpretation
        var evaluator = new Evaluator(childContext, new Dictionary<string, Func<object?[], object?>>());
        return evaluator.Evaluate(lambda.Body);
    }

    private static readonly Dictionary<string, Func<List<object?>, object?[], CsEvalContext, (bool, object?)>> LinqHandlers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Filtering
            ["Where"] = HandleWhere,
            ["Filter"] = HandleWhere, // JS alias

            // Projection
            ["Select"] = HandleSelect,
            ["Map"] = HandleSelect, // JS alias
            ["SelectMany"] = HandleSelectMany,
            ["FlatMap"] = HandleSelectMany, // JS alias

            // Element access
            ["First"] = HandleFirst,
            ["FirstOrDefault"] = HandleFirstOrDefault,
            ["Find"] = HandleFirstOrDefault, // JS alias
            ["Last"] = HandleLast,
            ["LastOrDefault"] = HandleLastOrDefault,
            ["Single"] = HandleSingle,
            ["SingleOrDefault"] = HandleSingleOrDefault,
            ["ElementAt"] = HandleElementAt,
            ["ElementAtOrDefault"] = HandleElementAtOrDefault,

            // Quantifiers
            ["Any"] = HandleAny,
            ["Some"] = HandleAny, // JS alias
            ["All"] = HandleAll,
            ["Every"] = HandleAll, // JS alias

            // Aggregation
            ["Count"] = HandleCount,
            ["Sum"] = HandleSum,
            ["Average"] = HandleAverage,
            ["Min"] = HandleMin,
            ["Max"] = HandleMax,
            ["MinBy"] = HandleMinBy,
            ["MaxBy"] = HandleMaxBy,
            ["Aggregate"] = HandleAggregate,
            ["Reduce"] = HandleReduce, // JS style

            // Ordering
            ["OrderBy"] = HandleOrderBy,
            ["OrderByDescending"] = HandleOrderByDescending,
            ["Reverse"] = HandleReverse,

            // Grouping
            ["GroupBy"] = HandleGroupBy,

            // Combining
            ["Zip"] = HandleZip,
            ["Concat"] = HandleConcat,

            // Set operations
            ["Except"] = HandleExcept,
            ["Intersect"] = HandleIntersect,
            ["Union"] = HandleUnion,

            // Partitioning
            ["Take"] = HandleTake,
            ["Skip"] = HandleSkip,

            // Other
            ["Distinct"] = HandleDistinct,
            ["Contains"] = HandleContains,
            ["Includes"] = HandleContains, // JS alias
            ["SequenceEqual"] = HandleSequenceEqual,
            ["DefaultIfEmpty"] = HandleDefaultIfEmpty,
            ["ToList"] = HandleToList,
            ["ToArray"] = HandleToArray,
            ["OfType"] = HandleOfType,
            ["Cast"] = HandleCast,
        };

    internal static bool IsLinqMethod(string methodName) => LinqHandlers.ContainsKey(methodName);

    internal static (bool Success, object? Value) TryInvokeEnumerableMethod(
        System.Collections.IEnumerable enumerable,
        string methodName,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options)
    {
        if (!LinqHandlers.TryGetValue(methodName, out var handler))
            return (false, null);

        var list = enumerable.Cast<object?>().ToList();
        return handler(list, args, context);
    }

    private static object? InvokeLambdaForLinq(LambdaValue lambda, object?[] args, CsEvalContext context)
    {
        var childContext = lambda.Closure.CreateChild();
        for (var i = 0; i < lambda.Parameters.Count && i < args.Length; i++)
            childContext.Define(lambda.Parameters[i], args[i]);
        var evaluator = new Evaluator(childContext, new Dictionary<string, Func<object?[], object?>>());
        return evaluator.Evaluate(lambda.Body);
    }

    private static (bool, object?) HandleWhere(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [LambdaValue predicate]) return (false, null);
        return (true, list.Where(item => RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx))).ToList());
    }

    private static (bool, object?) HandleSelect(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [LambdaValue selector]) return (false, null);
        return (true, list.Select(item => InvokeLambdaForLinq(selector, [item], ctx)).ToList());
    }

    private static (bool, object?) HandleSelectMany(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [LambdaValue selector]) return (false, null);
        return (true, list.SelectMany(item =>
        {
            var result = InvokeLambdaForLinq(selector, [item], ctx);
            if (result is System.Collections.IEnumerable ie and not string)
                return ie.Cast<object?>();
            throw new CsEvalException("SelectMany selector must return an enumerable");
        }).ToList());
    }

    private static (bool, object?) HandleFirst(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [LambdaValue predicate])
            return (true, list.First(item => RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx))));
        return (true, list.First());
    }

    private static (bool, object?) HandleFirstOrDefault(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [LambdaValue predicate])
            return (true, list.FirstOrDefault(item => RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx))));
        return (true, list.FirstOrDefault());
    }

    private static (bool, object?) HandleLast(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [LambdaValue predicate])
            return (true, list.Last(item => RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx))));
        return (true, list.Last());
    }

    private static (bool, object?) HandleLastOrDefault(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [LambdaValue predicate])
            return (true, list.LastOrDefault(item => RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx))));
        return (true, list.LastOrDefault());
    }

    private static (bool, object?) HandleSingle(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [LambdaValue predicate])
            return (true, list.Single(item => RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx))));
        return (true, list.Single());
    }

    private static (bool, object?) HandleSingleOrDefault(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [LambdaValue predicate])
            return (true, list.SingleOrDefault(item => RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx))));
        return (true, list.SingleOrDefault());
    }

    private static (bool, object?) HandleCount(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [LambdaValue predicate])
            return (true, list.Count(item => RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx))));
        return (true, list.Count);
    }

    private static (bool, object?) HandleAny(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [LambdaValue predicate])
            return (true, list.Any(item => RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx))));
        return (true, list.Any());
    }

    private static (bool, object?) HandleAll(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [LambdaValue predicate]) return (false, null);
        return (true, list.All(item => RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx))));
    }

    private static (bool, object?) HandleSum(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (list.Count == 0)
            return (true, 0);

        // Get first element to determine type and validate
        var first = args is [LambdaValue sel]
            ? InvokeLambdaForLinq(sel, [list.FirstOrDefault(x => x != null) ?? list[0]], ctx)
            : list.FirstOrDefault(x => x != null) ?? list[0];

        // Validate that Sum is only called on numeric types
        var typeCode = first == null ? TypeCode.Empty : Type.GetTypeCode(first.GetType());
        if (typeCode is < TypeCode.SByte or > TypeCode.Decimal)
            throw new InvalidOperationException($"Sum() requires numeric elements, but found '{first?.GetType().Name ?? "null"}'");

        // Initialize with typed zero, let C# handle arithmetic via dynamic
        dynamic sum = first switch { decimal => 0m, double => 0.0, float => 0f, long => 0L, _ => 0 };

        if (args is [LambdaValue selector])
        {
            foreach (var item in list)
                sum += (dynamic)InvokeLambdaForLinq(selector, [item], ctx)!;
        }
        else
        {
            foreach (var item in list)
                sum += (dynamic)item!;
        }

        return (true, (object)sum);
    }

    private static (bool, object?) HandleMin(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [LambdaValue selector])
            return (true, list.Min(item => InvokeLambdaForLinq(selector, [item], ctx)));
        return (true, list.Min());
    }

    private static (bool, object?) HandleMax(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [LambdaValue selector])
            return (true, list.Max(item => InvokeLambdaForLinq(selector, [item], ctx)));
        return (true, list.Max());
    }

    private static (bool, object?) HandleAverage(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");

        var first = args is [LambdaValue sel]
            ? InvokeLambdaForLinq(sel, [list.FirstOrDefault(x => x != null) ?? list[0]], ctx)
            : list.FirstOrDefault(x => x != null) ?? list[0];

        return first switch
        {
            decimal => args is [LambdaValue s]
                ? (true, list.Select(i => (decimal)InvokeLambdaForLinq(s, [i], ctx)!).Average())
                : (true, list.Cast<decimal>().Average()),
            float => args is [LambdaValue s]
                ? (true, list.Select(i => (float)InvokeLambdaForLinq(s, [i], ctx)!).Average())
                : (true, list.Cast<float>().Average()),
            double => args is [LambdaValue s]
                ? (true, list.Select(i => (double)InvokeLambdaForLinq(s, [i], ctx)!).Average())
                : (true, list.Cast<double>().Average()),
            long => args is [LambdaValue s]
                ? (true, list.Select(i => (long)InvokeLambdaForLinq(s, [i], ctx)!).Average())
                : (true, list.Cast<long>().Average()),
            _ => args is [LambdaValue s]
                ? (true, list.Select(i => (int)InvokeLambdaForLinq(s, [i], ctx)!).Average())
                : (true, list.Cast<int>().Average())
        };
    }

    private static (bool, object?) HandleTake(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [var countObj] || countObj is not int count) return (false, null);
        return (true, list.Take(count).ToList());
    }

    private static (bool, object?) HandleSkip(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [var countObj] || countObj is not int count) return (false, null);
        return (true, list.Skip(count).ToList());
    }

    private static (bool, object?) HandleOrderBy(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [LambdaValue keySelector])
            return (true, list.OrderBy(item => InvokeLambdaForLinq(keySelector, [item], ctx)).ToList());
        return (true, list.OrderBy(x => x).ToList());
    }

    private static (bool, object?) HandleOrderByDescending(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [LambdaValue keySelector])
            return (true, list.OrderByDescending(item => InvokeLambdaForLinq(keySelector, [item], ctx)).ToList());
        return (true, list.OrderByDescending(x => x).ToList());
    }

    private static (bool, object?) HandleDistinct(List<object?> list, object?[] args, CsEvalContext ctx)
        => (true, list.Distinct().ToList());

    private static (bool, object?) HandleReverse(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        var result = new List<object?>(list);
        result.Reverse();
        return (true, result);
    }

    private static (bool, object?) HandleToList(List<object?> list, object?[] args, CsEvalContext ctx)
        => (true, list);

    private static (bool, object?) HandleToArray(List<object?> list, object?[] args, CsEvalContext ctx)
        => (true, list.ToArray());

    private static (bool, object?) HandleConcat(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [System.Collections.IEnumerable other]) return (false, null);
        return (true, list.Concat(other.Cast<object?>()).ToList());
    }

    private static (bool, object?) HandleExcept(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [System.Collections.IEnumerable other]) return (false, null);
        return (true, list.Except(other.Cast<object?>()).ToList());
    }

    private static (bool, object?) HandleUnion(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [System.Collections.IEnumerable other]) return (false, null);
        return (true, list.Union(other.Cast<object?>()).ToList());
    }

    private static (bool, object?) HandleIntersect(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [System.Collections.IEnumerable other]) return (false, null);
        return (true, list.Intersect(other.Cast<object?>()).ToList());
    }

    private static (bool, object?) HandleZip(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        // With selector: zip(other, (a, b) => ...)
        if (args is [System.Collections.IEnumerable other and not string, LambdaValue selector])
            return (true, list.Zip(other.Cast<object?>(), (a, b) => InvokeLambdaForLinq(selector, [a, b], ctx)).ToList());

        // Without selector: zip(other) - returns { First, Second } dictionaries
        if (args is [System.Collections.IEnumerable zipOther and not string])
        {
            var otherList = zipOther.Cast<object?>().ToList();
            return (true, list.Zip(otherList, (first, second) => (object?)new Dictionary<string, object?>
            {
                ["First"] = first,
                ["Second"] = second
            }).ToList());
        }

        return (false, null);
    }

    private static (bool, object?) HandleContains(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [var item]) return (false, null);
        return (true, list.Contains(item));
    }

    private static (bool, object?) HandleSequenceEqual(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [System.Collections.IEnumerable other]) return (false, null);
        return (true, list.SequenceEqual(other.Cast<object?>()));
    }

    private static (bool, object?) HandleAggregate(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [LambdaValue func])
            return (true, list.Aggregate((acc, item) => InvokeLambdaForLinq(func, [acc, item], ctx)));
        if (args is [var seed, LambdaValue func2])
            return (true, list.Aggregate(seed, (acc, item) => InvokeLambdaForLinq(func2, [acc, item], ctx)));
        return (false, null);
    }

    private static (bool, object?) HandleGroupBy(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [LambdaValue keySelector]) return (false, null);
        var groups = list.GroupBy(item => InvokeLambdaForLinq(keySelector, [item], ctx));
        return (true, groups.Select(g => (object?)new Dictionary<string, object?>
        {
            ["Key"] = g.Key,
            ["Items"] = g.ToList()
        }).ToList());
    }

    private static (bool, object?) HandleElementAt(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [var indexObj]) return (false, null);
        var index = Convert.ToInt32(indexObj);
        return (true, list.ElementAt(index));
    }

    private static (bool, object?) HandleElementAtOrDefault(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [var indexObj]) return (false, null);
        var index = Convert.ToInt32(indexObj);
        return (true, list.ElementAtOrDefault(index));
    }

    private static (bool, object?) HandleDefaultIfEmpty(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [var defaultValue])
            return (true, list.DefaultIfEmpty(defaultValue).ToList());
        return (true, list.DefaultIfEmpty().ToList());
    }

    private static (bool, object?) HandleOfType(List<object?> list, object?[] args, CsEvalContext ctx)
        => (true, list.Where(x => x != null).ToList());

    private static (bool, object?) HandleCast(List<object?> list, object?[] args, CsEvalContext ctx)
        => (true, list);

    private static (bool, object?) HandleMinBy(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [LambdaValue selector]) return (false, null);
        if (list.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");
        return (true, list.MinBy(item => InvokeLambdaForLinq(selector, [item], ctx)));
    }

    private static (bool, object?) HandleMaxBy(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is not [LambdaValue selector]) return (false, null);
        if (list.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");
        return (true, list.MaxBy(item => InvokeLambdaForLinq(selector, [item], ctx)));
    }

    private static (bool, object?) HandleReduce(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        // JS style with seed: reduce((acc, item) => ..., seed)
        if (args is [LambdaValue reducer, var seed])
            return (true, list.Aggregate(seed, (acc, item) => InvokeLambdaForLinq(reducer, [acc, item], ctx)));

        // Without seed: reduce((acc, item) => ...) - uses first element as seed
        if (args is [LambdaValue reducerOnly])
            return (true, list.Skip(1).Aggregate(list.FirstOrDefault(), (acc, item) => InvokeLambdaForLinq(reducerOnly, [acc, item], ctx)));

        return (false, null);
    }
}
