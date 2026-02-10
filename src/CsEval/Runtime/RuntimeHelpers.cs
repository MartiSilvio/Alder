using CsEval.Interpretation;

namespace CsEval.Runtime;

public static class RuntimeHelpers
{
    public static object? ResolveIdentifier(string name, CsEvalContext context)
    {
        if (context.Functions.TryGetValue(name, out var function))
            return new FunctionRef(name, function);

        if (context.Modules.TryGetValue(name, out var module))
            return module;

        return context.Get(name);
    }

    public static void CheckAllowAssignment(CsEvalOptions options, string context)
    {
        if (!options.Sandbox.AllowAssignment)
            throw new CsEvalException($"Assignment blocked by sandbox: {context}");
    }

    public static void CheckNullCoalesceAssignAllowed(string name, CsEvalContext context)
    {
        if (context.TryGetVariableType(name, out var varType) && varType != null && !TypeHelpers.IsNullableType(varType))
            throw new CsEvalException($"Operator '??=' cannot be applied to operand of type '{varType.Name}'");
    }

    public static void CheckAllowIndexSet(CsEvalOptions options, object? index)
    {
        if (!options.Sandbox.AllowIndexSet)
            throw new CsEvalException($"Index assignment blocked by sandbox: [{index}] = ...");
    }

    /// <summary>
    /// ECMA-334 §12.21.4: Compound assignment is evaluated as x = (T)(x op y).
    /// The result of the binary operation is explicitly cast back to the variable's
    /// declared type. This is allowed only when y is implicitly convertible to T
    /// (or the result type is already compatible with T).
    /// Uses TypeHelpers.RuntimeCast for correct C# cast semantics instead of
    /// Convert.ChangeType which fails for char and nullable types.
    /// </summary>
    public static object? ValidateCompoundAssignment(string name, object? result, object? rightValue, CsEvalContext context)
    {
        if (!context.TryGetVariableType(name, out var varType) || varType == null || result == null)
            return result;

        var resultType = result.GetType();
        var rightType = rightValue?.GetType();

        // If result already matches the declared type, no conversion needed
        if (resultType == varType)
            return result;

        // ECMA-334 §12.21.4: compound assignment x op= y requires either:
        // 1. y is implicitly convertible to the type of x, OR
        // 2. the result type is implicitly convertible to x's type
        // Additionally, ECMA-334 §10.2.11 allows implicit constant expression
        // conversions: an int value that fits in the target integer type is allowed.
        // Since CsEval cannot distinguish constants from runtime values, we treat int
        // RHS values that fit in the target integer range as implicitly convertible.
        var underlyingVarType = Nullable.GetUnderlyingType(varType) ?? varType;
        var rhsConvertible = rightType == null || rightType == varType ||
                             TypeHelpers.CanImplicitlyConvert(rightType, underlyingVarType) ||
                             IsConstantExpressionConvertible(rightValue, rightType, underlyingVarType);
        var resultConvertible = TypeHelpers.CanImplicitlyConvert(resultType, underlyingVarType);

        if (!resultConvertible && !rhsConvertible)
        {
            throw new CsEvalException($"Cannot implicitly convert type '{resultType.Name}' to '{varType.Name}'");
        }

        // Get the underlying type for nullable targets
        var targetType = Nullable.GetUnderlyingType(varType) ?? varType;
        try
        {
            return TypeHelpers.RuntimeCast(result, resultType, targetType);
        }
        catch (Exception ex) when (ex is InvalidCastException or OverflowException)
        {
            throw new CsEvalException($"Cannot implicitly convert type '{resultType.Name}' to '{varType.Name}'");
        }
    }

    /// <summary>
    /// ECMA-334 §10.2.11: Checks if a value qualifies for implicit constant
    /// expression conversion. An int value can be implicitly converted to sbyte, byte,
    /// short, ushort, uint, ulong, or char if it fits in the target range.
    /// </summary>
    private static bool IsConstantExpressionConvertible(object? value, Type? sourceType, Type targetType)
    {
        if (value == null || sourceType != typeof(int))
            return false;

        var intVal = (int)value;

        if (targetType == typeof(sbyte)) return intVal is >= sbyte.MinValue and <= sbyte.MaxValue;
        if (targetType == typeof(byte)) return intVal is >= byte.MinValue and <= byte.MaxValue;
        if (targetType == typeof(short)) return intVal is >= short.MinValue and <= short.MaxValue;
        if (targetType == typeof(ushort)) return intVal is >= ushort.MinValue and <= ushort.MaxValue;
        if (targetType == typeof(uint)) return intVal >= 0;
        if (targetType == typeof(ulong)) return intVal >= 0;
        if (targetType == typeof(char)) return intVal is >= char.MinValue and <= char.MaxValue;

        return false;
    }

    public static void CheckIterationLimit(long iterations, CsEvalOptions options)
    {
        if (options.MaxIterations > 0 && iterations > options.MaxIterations)
            throw new CsEvalException($"Loop exceeded maximum iterations ({options.MaxIterations}). Possible infinite loop.");
    }

    public static IEnumerator GetEnumerator(object? collection)
    {
        if (collection is not IEnumerable enumerable)
            throw new CsEvalException($"Cannot iterate over type '{collection?.GetType().Name ?? "null"}' in foreach");

        return enumerable.GetEnumerator();
    }

    public static object? GetLambdaArg(object?[] args, int index)
    {
        return index < args.Length ? args[index] : null;
    }

    /// <summary>
    /// Invokes a constructor for the given type with the given arguments.
    /// ECMA-334 §12.8.16.2 - Object creation expressions.
    /// </summary>
    public static object? InvokeConstructor(Type type, object?[] args)
    {
        try
        {
            if (args.Length == 0)
                return Activator.CreateInstance(type);

            return Activator.CreateInstance(type, args);
        }
        catch (MissingMethodException)
        {
            throw new CsEvalException($"No matching constructor found for '{type.Name}' with {args.Length} argument(s)");
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>
    /// Creates a System.ValueTuple from an array of elements.
    /// ECMA-334 §12.8.6 - Tuple expressions.
    /// Supports 2-7 element tuples.
    /// </summary>
    public static object CreateTuple(object?[] elements)
    {
        if (elements.Length < 2)
            throw new CsEvalException("Tuples must have at least 2 elements");
        if (elements.Length > 7)
            throw new CsEvalException("Tuples with more than 7 elements are not supported");

        var types = new Type[elements.Length];
        for (var i = 0; i < elements.Length; i++)
            types[i] = elements[i]?.GetType() ?? typeof(object);

        var openGenericType = elements.Length switch
        {
            2 => typeof(ValueTuple<,>),
            3 => typeof(ValueTuple<,,>),
            4 => typeof(ValueTuple<,,,>),
            5 => typeof(ValueTuple<,,,,>),
            6 => typeof(ValueTuple<,,,,,>),
            7 => typeof(ValueTuple<,,,,,,>),
            _ => throw new CsEvalException("Tuples with more than 7 elements are not supported")
        };

        var tupleType = openGenericType.MakeGenericType(types);
        return Activator.CreateInstance(tupleType, elements)!;
    }

    /// <summary>
    /// Deconstructs a tuple into individual variables in the given context.
    /// ECMA-334 §12.7 - Deconstruction.
    /// </summary>
    public static object? DeconstructTuple(object? tupleValue, string[] variableNames, CsEvalContext context)
    {
        if (tupleValue is not System.Runtime.CompilerServices.ITuple tuple)
            throw new CsEvalException($"Cannot deconstruct non-tuple value of type '{tupleValue?.GetType().Name ?? "null"}'");
        if (tuple.Length != variableNames.Length)
            throw new CsEvalException($"Deconstruction requires {variableNames.Length} values but tuple has {tuple.Length} elements");
        for (var i = 0; i < variableNames.Length; i++)
        {
            var elementValue = tuple[i];
            var elementType = elementValue?.GetType() ?? typeof(object);
            context.DefineNew(variableNames[i], elementValue, elementType);
        }
        return tupleValue;
    }

    /// <summary>
    /// Creates a typed array from an element type and size.
    /// ECMA-334 §12.8.16.4 - Array creation expressions.
    /// Used by the IL compiler as a single method call target for typed array creation.
    /// </summary>
    public static object CreateTypedArray(Type elementType, object sizeValue)
    {
        var size = Convert.ToInt32(sizeValue);
        return Array.CreateInstance(elementType, size);
    }

    /// <summary>
    /// ECMA-334 §12.18: Promotes conditional expression result to common type of both branches.
    /// </summary>
    public static object? ConditionalTypePromotion(object? result, object? thenValue, object? elseValue)
    {
        if (thenValue == null || elseValue == null || result == null)
            return result;

        if (!TypeHelpers.IsArithmetic(thenValue) || !TypeHelpers.IsArithmetic(elseValue))
            return result;

        var thenType = thenValue.GetType();
        var elseType = elseValue.GetType();

        if (thenType == elseType)
            return result;

        var resultType = NumericDispatch.GetResultType(thenType, elseType);
        return NumericDispatch.PromoteToType(result, resultType);
    }
}
