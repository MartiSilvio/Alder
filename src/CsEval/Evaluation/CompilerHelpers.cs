using System.Reflection;

namespace CsEval.Evaluation;

/// <summary>
/// Static helper methods called by compiled expressions.
/// These mirror the behavior in Evaluator.Operators.cs for consistency.
/// </summary>
public static class CompilerHelpers
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

        throw new EvalException($"Cannot negate {value?.GetType().Name ?? "null"}");
    }

    public static object? Add(object? left, object? right, CsEvalOptions options)
    {
        // String concatenation
        if (left is string || right is string)
            return $"{left}{right}";

        // Let C# runtime handle numeric addition via dynamic
        if (IsNumeric(left) && IsNumeric(right))
            return (dynamic)left! + (dynamic)right!;

        // Object merging not supported in compiled expressions
        throw new EvalException(
            $"Cannot add {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"} in compiled expression. " +
            "Object merging requires tree-walking evaluation.");
    }

    public static object? Subtract(object? left, object? right, CsEvalOptions options)
    {
        if (IsNumeric(left) && IsNumeric(right))
            return (dynamic)left! - (dynamic)right!;

        throw new EvalException($"Cannot subtract {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object? Multiply(object? left, object? right, CsEvalOptions options)
    {
        if (IsNumeric(left) && IsNumeric(right))
            return (dynamic)left! * (dynamic)right!;

        throw new EvalException($"Cannot multiply {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object? Divide(object? left, object? right, CsEvalOptions options)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            if ((dynamic)right! == 0)
                throw new DivideByZeroException();
            return (dynamic)left! / (dynamic)right!;
        }

        throw new EvalException($"Cannot divide {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object? Modulo(object? left, object? right, CsEvalOptions options)
    {
        if (IsNumeric(left) && IsNumeric(right))
        {
            if ((dynamic)right! == 0)
                throw new DivideByZeroException();
            return (dynamic)left! % (dynamic)right!;
        }

        throw new EvalException($"Cannot modulo {left?.GetType().Name ?? "null"} and {right?.GetType().Name ?? "null"}");
    }

    public static object Equals(object? left, object? right, CsEvalOptions options)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        if (left.Equals(right)) return true;

        // Let C# runtime handle numeric comparison via dynamic
        if (IsNumeric(left) && IsNumeric(right))
            return (dynamic)left! == (dynamic)right!;

        return false;
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

    private static int Compare(object? left, object? right, CsEvalOptions options)
    {
        if (left == null || right == null)
            throw new EvalException("Cannot compare null values");

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
            _ => throw new EvalException($"Cannot compare {left.GetType().Name} and {right.GetType().Name}")
        };
    }

    public static object? GetMember(object? obj, string name, CsEvalOptions options, bool nullSafe, EvalContext context)
    {
        if (nullSafe && obj == null)
            return null;

        if (obj == null)
            throw new EvalException($"Cannot access property '{name}' on null");

        if (!options.Sandbox.AllowPropertyRead)
            throw new EvalException($"Property access blocked by sandbox: {name}");

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
                    _ => throw new EvalException($"Member '{name}' is not a property or field")
                };
                return GuardReflectionLeak(value, $"property {name}");
            }
            // Member not in dictionary - not allowed (matches tree-walking evaluator behavior)
            throw new EvalException($"Member '{name}' not found on module '{resolver.Type.Name}'");
        }

        if (obj is IDictionary<string, object?> dict)
        {
            if (dict.TryGetValue(name, out var value))
                return GuardReflectionLeak(value, $"property {name}");

            if (options.IgnoreCase)
            {
                foreach (var key in dict.Keys)
                {
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        return GuardReflectionLeak(dict[key], $"property {name}");
                }
            }

            throw new EvalException($"Property '{name}' not found");
        }

        var type = obj.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (options.IgnoreCase)
            bindingFlags |= BindingFlags.IgnoreCase;

        var typeCache = context.TypeCache;
        var prop = typeCache.GetProperty(type, name, bindingFlags);
        if (prop != null)
            return GuardReflectionLeak(typeCache.GetPropertyValue(prop, obj), $"property {name}");

        var field = typeCache.GetField(type, name, bindingFlags);
        if (field != null)
            return GuardReflectionLeak(field.GetValue(obj), $"field {name}");

        throw new EvalException($"Property '{name}' not found on type '{type.Name}'");
    }

    /// <summary>
    /// Checks if a type is a forbidden reflection metadata type.
    /// </summary>
    private static bool IsForbiddenReflectionType(Type? type)
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
    private static object? GuardReflectionLeak(object? value, string context)
    {
        if (value == null) return null;

        var type = value.GetType();
        if (IsForbiddenReflectionType(type))
        {
            throw new EvalException($"Access to reflection types is not allowed: {type.Name} ({context})");
        }

        return value;
    }

    public static object? BitwiseAnd(object? left, object? right, CsEvalOptions options)
    {
        if (IsInteger(left) && IsInteger(right))
            return (dynamic)left! & (dynamic)right!;
        
        if (left is bool lb && right is bool rb)
            return lb & rb;

        throw new EvalException($"Cannot apply operator & to {left?.GetType().Name} and {right?.GetType().Name}");
    }

    public static object? BitwiseOr(object? left, object? right, CsEvalOptions options)
    {
        if (IsInteger(left) && IsInteger(right))
            return (dynamic)left! | (dynamic)right!;

        if (left is bool lb && right is bool rb)
            return lb | rb;

        throw new EvalException($"Cannot apply operator | to {left?.GetType().Name} and {right?.GetType().Name}");
    }

    public static object? BitwiseXor(object? left, object? right, CsEvalOptions options)
    {
        if (IsInteger(left) && IsInteger(right))
            return (dynamic)left! ^ (dynamic)right!;

        if (left is bool lb && right is bool rb)
            return lb ^ rb;

        throw new EvalException($"Cannot apply operator ^ to {left?.GetType().Name} and {right?.GetType().Name}");
    }

    private static bool IsInteger(object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong;

    private static bool IsNumeric(object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

    /// <summary>
    /// Checks if assignment is allowed by sandbox. Throws if not.
    /// </summary>
    public static void CheckAllowAssignment(CsEvalOptions options, string context)
    {
        if (!options.Sandbox.AllowAssignment)
            throw new EvalException($"Assignment blocked by sandbox: {context}");
    }
}
