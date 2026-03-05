using System.Runtime.CompilerServices;
using CsEval.Diagnostics;
using CsEval.Interpretation;
using CsEval.Parsing;
using CsEval.Runtime.Extensions;

namespace CsEval.Runtime;

internal static class RuntimeHelpers
{
    public static object? ResolveIdentifier(string name, CsEvalContext context, CsEvalOptions options)
    {
        if (context.Functions.TryGetValue(name, out var function))
            return new FunctionRef(name, function);

        if (context.Modules.TryGetValue(name, out var module))
            return module;

        // Try variable lookup first (fast path for common case)
        if (context.TryGet(name, out var value))
            return value;

        // If not a variable/function/module, check if it's a namespace prefix.
        // This enables FQN type access like System.Linq.Enumerable.Where(...)
        if (context.TypeResolver.IsNamespaceOrPrefix(name))
            return new NamespaceRef(name);

        // Try resolving as a type name for static member access
        // Enables: Array.Empty<int>(), Math.Max(1, 2), Enumerable.Range(0, 10)
        var resolvedType = context.TypeResolver.TryResolveType(name);
        if (resolvedType != null)
            return resolvedType;

        // Bare math constants in Extended mode (pi, e, tau, infinity, nan)
        // User variables shadow these -- checked above via TryGet
        if (options.LanguageMode == LanguageMode.Extended &&
            BareMathNames.TryGetConstant(name, out var constant))
            return constant;

        // Fall through to context.Get which throws CS0103 with proper error message
        return context.Get(name);
    }

    /// <summary>
    /// Resolves a bare math function call in Extended mode.
    /// Called from the compiled path when the callee is an unresolved identifier.
    /// Tries bare math function lookup before falling back to the normal call path.
    /// </summary>
    public static object? InvokeBareMathOrCall(
        string name,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct,
        IReadOnlyList<string>? typeArgs)
    {
        // Only intercept in Extended mode when name is not in user scope
        if (options.LanguageMode == LanguageMode.Extended &&
            !context.TryGet(name, out _) &&
            !context.Functions.ContainsKey(name) &&
            BareMathNames.TryGetFunction(name, args.Length, out var mathFunc))
        {
            return mathFunc(args);
        }

        // Fall back to normal resolution + call
        var callee = ResolveIdentifier(name, context, options);
        return MethodInvoker.InvokeCall(callee, args, context, options, ct, typeArgs);
    }

    public static void CheckAllowAssignment(CsEvalOptions options, string context)
    {
        if (!options.Sandbox.AllowAssignment)
            throw new CsEvalException($"Assignment blocked by sandbox: {context}");
    }

    public static void CheckNullCoalesceAssignAllowed(string name, CsEvalContext context)
    {
        if (context.TryGetVariableType(name, out var varType) && varType != null && !TypeHelpers.IsNullableType(varType))
            throw new CsEvalException(DiagnosticDescriptors.BadBinaryOps, "??=", varType.Name, varType.Name);
    }

    public static void DisposeResource(object? resource)
    {
        if (resource is IDisposable d)
            d.Dispose();
        else if (resource is IAsyncDisposable asyncD)
            asyncD.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public static object ValidateLockObject(object? lockObj)
    {
        if (lockObj == null)
            throw new CsEvalException("lock statement requires a non-null reference");

        return lockObj;
    }

    public static Exception ValidateThrowOperand(object? value)
    {
        if (value is Exception ex)
            return ex;

        throw new CsEvalException(
            DiagnosticDescriptors.ThrowExpressionMustBeException);
    }

    public static bool EvaluateCatchWhenGuard(
        Expr guardExpression,
        string? catchVariableName,
        object? caughtException,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken ct)
    {
        var guardContext = context.CreateChild();
        if (!string.IsNullOrEmpty(catchVariableName))
            guardContext.Define(catchVariableName, caughtException);

        try
        {
            var evaluator = new Evaluator(guardContext, options, ct);
            var guardResult = evaluator.Evaluate(guardExpression);
            return TypeHelpers.RequireBoolean(guardResult);
        }
        catch
        {
            // ECMA-334: exceptions thrown during catch filter evaluation
            // are treated as filter-false.
            return false;
        }
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
            throw new CsEvalException(DiagnosticDescriptors.NoImplicitConversion, resultType.Name, varType.Name);
        }

        // Get the underlying type for nullable targets
        var targetType = Nullable.GetUnderlyingType(varType) ?? varType;
        try
        {
            return TypeHelpers.RuntimeCast(result, resultType, targetType);
        }
        catch (Exception ex) when (ex is InvalidCastException or OverflowException)
        {
            throw new CsEvalException(DiagnosticDescriptors.NoImplicitConversion, resultType.Name, varType.Name);
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

    /// <summary>
    /// Unified execution constraint check. Called at statement boundaries in both
    /// interpreted and compiled paths. Checks CancellationToken, MaxStatements,
    /// and MaxTimeout in a single call.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckExecutionConstraints(
        ExecutionConstraintState? state,
        ExecutionConstraints? constraints,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (state == null || constraints == null) return;

        state.StatementCount++;

        if (constraints.MaxStatements is { } maxStmts && maxStmts > 0
            && state.StatementCount > maxStmts)
        {
            throw new CsEvalExecutionLimitException(
                ExecutionLimitType.Statements,
                maxStmts,
                state.StatementCount,
                state.StatementCount,
                state.Timer?.Elapsed ?? TimeSpan.Zero);
        }

        if (state.Timer != null
            && constraints.MaxTimeout is { } maxTimeout
            && state.Timer.Elapsed > maxTimeout)
        {
            throw new CsEvalExecutionLimitException(
                ExecutionLimitType.Timeout,
                (long)maxTimeout.TotalMilliseconds,
                (long)state.Timer.ElapsedMilliseconds,
                state.StatementCount,
                state.Timer.Elapsed);
        }
    }

    public static IEnumerator GetEnumerator(object? collection)
    {
        if (collection is not IEnumerable enumerable)
            throw new CsEvalException(DiagnosticDescriptors.ForeachRequiresIEnumerable, collection?.GetType().Name ?? "null");

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
            throw new CsEvalException(DiagnosticDescriptors.NoMatchingConstructor, type.Name, args.Length);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>
    /// Creates a System.ValueTuple from an array of elements.
    /// ECMA-334 §12.8.6 - Tuple expressions.
    /// Supports tuples with 2+ elements (arity > 7 uses nested Rest tuple).
    /// </summary>
    public static object CreateTuple(object?[] elements)
    {
        if (elements.Length == 0)
            throw new CsEvalException("Tuples must have at least 2 elements");

        if (elements.Length <= 7)
        {
            var types = new Type[elements.Length];
            for (var i = 0; i < elements.Length; i++)
                types[i] = elements[i]?.GetType() ?? typeof(object);

            var openGenericType = elements.Length switch
            {
                1 => typeof(ValueTuple<>),
                2 => typeof(ValueTuple<,>),
                3 => typeof(ValueTuple<,,>),
                4 => typeof(ValueTuple<,,,>),
                5 => typeof(ValueTuple<,,,,>),
                6 => typeof(ValueTuple<,,,,,>),
                7 => typeof(ValueTuple<,,,,,,>),
                _ => throw new CsEvalException("Tuples must have at least 2 elements")
            };

            var tupleType = openGenericType.MakeGenericType(types);
            return Activator.CreateInstance(tupleType, elements)!;
        }

        var restElements = elements[7..];
        var restTuple = CreateTuple(restElements);
        var genericArgs = new Type[8];
        var ctorArgs = new object?[8];

        for (var i = 0; i < 7; i++)
        {
            genericArgs[i] = elements[i]?.GetType() ?? typeof(object);
            ctorArgs[i] = elements[i];
        }

        genericArgs[7] = restTuple.GetType();
        ctorArgs[7] = restTuple;

        var nestedTupleType = typeof(ValueTuple<,,,,,,,>).MakeGenericType(genericArgs);
        return Activator.CreateInstance(nestedTupleType, ctorArgs)!;
    }

    /// <summary>
    /// Deconstructs a value into individual variables in the given context.
    /// Supports ITuple (ValueTuple) and types with a Deconstruct() method.
    /// ECMA-334 §12.7 - Deconstruction.
    /// </summary>
    public static object? DeconstructTuple(object? tupleValue, string[] variableNames, CsEvalContext context)
    {
        // ITuple path (ValueTuple)
        if (tupleValue is System.Runtime.CompilerServices.ITuple tuple)
        {
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

        // Deconstruct() method path
        if (tupleValue is not null)
        {
            var deconstructed = TryDeconstruct(tupleValue, variableNames.Length);
            if (deconstructed != null)
            {
                for (var i = 0; i < variableNames.Length; i++)
                {
                    var elementValue = deconstructed[i];
                    var elementType = elementValue?.GetType() ?? typeof(object);
                    context.DefineNew(variableNames[i], elementValue, elementType);
                }
                return tupleValue;
            }
        }

        throw new CsEvalException($"Cannot deconstruct value of type '{tupleValue?.GetType().Name ?? "null"}': no ITuple implementation or Deconstruct() method found");
    }

    /// <summary>
    /// Tries to call a Deconstruct() method on the given value with the specified number of out parameters.
    /// Returns the deconstructed values as an array, or null if no matching Deconstruct() method is found.
    /// ECMA-334 §12.7 - Deconstruction via Deconstruct() method.
    /// </summary>
    public static object?[]? TryDeconstruct(object value, int parameterCount)
    {
        var type = value.GetType();
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "Deconstruct"
                        && m.GetParameters().Length == parameterCount
                        && m.GetParameters().All(p => p.IsOut))
            .ToArray();

        if (methods.Length == 0)
            return null;

        var method = methods[0];
        var args = new object?[parameterCount];
        method.Invoke(value, args);
        return args;
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
    /// Converts a source array to a typed T[] array.
    /// Accepts any array type (object?[], int[], etc.) as the source.
    /// Used for typed array literals: new int[] {1, 2, 3}
    /// </summary>
    public static object ConvertArrayToTyped(object sourceArrayObj, Type elementType)
    {
        var sourceArray = (Array)sourceArrayObj;
        var typedArray = Array.CreateInstance(elementType, sourceArray.Length);
        for (var i = 0; i < sourceArray.Length; i++)
        {
            typedArray.SetValue(sourceArray.GetValue(i), i);
        }
        return typedArray;
    }

    /// <summary>
    /// Applies a property initializer entry to an object: obj.PropertyName = value.
    /// Used by the IL compiler for object initializers.
    /// </summary>
    public static object? ApplyPropertyInitializer(object obj, string propertyName, object? value, CsEvalOptions options, CsEvalContext context)
    {
        MemberAccess.SetMember(obj, propertyName, value, options, context);
        return obj;
    }

    /// <summary>
    /// Applies a collection initializer entry to an object: obj.Add(value).
    /// Used by the IL compiler for collection initializers.
    /// </summary>
    public static object? ApplyCollectionInitializer(object obj, object? value)
    {
        var addMethod = obj.GetType().GetMethod("Add");
        if (addMethod != null)
            addMethod.Invoke(obj, new[] { value });
        else
            throw new CsEvalException($"Type '{obj.GetType().Name}' does not have an 'Add' method for collection initializer");
        return obj;
    }

    /// <summary>
    /// Creates a multi-dimensional array from an element type and sizes.
    /// Used by the IL compiler for expressions like new int[3, 3].
    /// </summary>
    public static object CreateMultiDimArray(Type elementType, object[] sizes)
    {
        var intSizes = new int[sizes.Length];
        for (var i = 0; i < sizes.Length; i++)
            intSizes[i] = Convert.ToInt32(sizes[i]);
        return Array.CreateInstance(elementType, intSizes);
    }

    /// <summary>
    /// Gets a value from a multi-dimensional array at the given indices.
    /// Used by the IL compiler for expressions like arr[i, j].
    /// </summary>
    public static object? MultiDimArrayGet(object arrayObj, object[] indices)
    {
        var targetType = arrayObj.GetType();
        if (arrayObj is not Array arr)
        {
            if (indices.Length > 1 && HasIndexerWithArity(targetType, indices.Length))
                throw new CsEvalException(DiagnosticDescriptors.MultiParameterIndexerNotSupported, targetType.Name);

            throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, targetType.Name);
        }

        try
        {
            var intIndices = new int[indices.Length];
            for (var i = 0; i < indices.Length; i++)
                intIndices[i] = Convert.ToInt32(indices[i]);
            return arr.GetValue(intIndices);
        }
        catch (CsEvalException)
        {
            throw;
        }
        catch
        {
            throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, arr.GetType().Name);
        }
    }

    /// <summary>
    /// Sets a value in a multi-dimensional array at the given indices. Returns the value.
    /// Used by the IL compiler for expressions like arr[i, j] = value.
    /// </summary>
    public static object? MultiDimArraySet(object arrayObj, object[] indices, object? value)
    {
        var targetType = arrayObj.GetType();
        if (arrayObj is not Array arr)
        {
            if (indices.Length > 1 && HasIndexerWithArity(targetType, indices.Length))
                throw new CsEvalException(DiagnosticDescriptors.MultiParameterIndexerNotSupported, targetType.Name);

            throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, targetType.Name);
        }

        try
        {
            var intIndices = new int[indices.Length];
            for (var i = 0; i < indices.Length; i++)
                intIndices[i] = Convert.ToInt32(indices[i]);
            arr.SetValue(value, intIndices);
            return value;
        }
        catch (CsEvalException)
        {
            throw;
        }
        catch
        {
            throw new CsEvalException(DiagnosticDescriptors.BadIndexerAccess, arr.GetType().Name);
        }
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

    private static bool HasIndexerWithArity(Type targetType, int parameterCount)
    {
        var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            if (property.Name == "Item" && property.GetIndexParameters().Length == parameterCount)
                return true;
        }

        return false;
    }
}
