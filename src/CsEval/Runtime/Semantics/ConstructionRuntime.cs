using System.Reflection;
using System.Linq;
using CsEval.Diagnostics;

namespace CsEval.Runtime;

internal static class ConstructionRuntime
{
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

    public static object? DeconstructTuple(object? tupleValue, string[] variableNames, CsEvalContext context)
    {
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

        throw new CsEvalException($"Cannot deconstruct value of type '{TypeNameFormatter.Of(tupleValue)}': no ITuple implementation or Deconstruct() method found");
    }

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

    public static object CreateTypedArray(Type elementType, object sizeValue)
    {
        var size = Convert.ToInt32(sizeValue);
        return Array.CreateInstance(elementType, size);
    }

    public static object ConvertArrayToTyped(object sourceArrayObj, Type elementType)
    {
        var sourceArray = (Array)sourceArrayObj;
        var typedArray = Array.CreateInstance(elementType, sourceArray.Length);
        for (var i = 0; i < sourceArray.Length; i++)
            typedArray.SetValue(sourceArray.GetValue(i), i);
        return typedArray;
    }

    public static object? ApplyPropertyInitializer(object obj, string propertyName, object? value, CsEvalOptions options, CsEvalContext context)
    {
        MemberAccess.SetMember(obj, propertyName, value, options, context);
        return obj;
    }

    public static object? ApplyCollectionInitializer(object obj, object? value)
    {
        var addMethod = obj.GetType().GetMethod("Add");
        if (addMethod != null)
            addMethod.Invoke(obj, new[] { value });
        else
            throw new CsEvalException($"Type '{obj.GetType().Name}' does not have an 'Add' method for collection initializer");
        return obj;
    }

    public static object CreateMultiDimArray(Type elementType, object[] sizes)
    {
        var intSizes = new int[sizes.Length];
        for (var i = 0; i < sizes.Length; i++)
            intSizes[i] = Convert.ToInt32(sizes[i]);
        return Array.CreateInstance(elementType, intSizes);
    }

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
