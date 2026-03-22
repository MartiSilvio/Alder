using System.Collections.Concurrent;
using System.Linq.Expressions;
using Alder.Diagnostics;

namespace Alder.Runtime.Semantics;

internal static class ConstructionRuntime
{
    private static readonly ConcurrentDictionary<Type, Action<object, object?>> _addInvokerCache = new();

    public static object? InvokeConstructor(Type type, object?[] args, AlderConfig config, AlderOptions? options = null)
    {
        if (options != null)
        {
            if (!options.Sandbox.AllowConstruction)
                throw new AlderException(DiagnosticDescriptors.SandboxConstructionBlocked, type.Name);

            if (!options.Sandbox.IsTypeAllowed(type))
                throw new AlderException(DiagnosticDescriptors.SandboxTypeBlocked, type.Name);
        }

        if (config.AotMetadata is { } aotCtorMeta && aotCtorMeta.TryGetValue(type, out var ctorMetadata))
        {
            try
            {
                if (ctorMetadata.TryCreateInstance(args, out var aotInstance))
                    return aotInstance;
            }
            catch (InvalidCastException)
            {
                // Generated dispatch matches on param count only; reflection resolves by type
            }
        }

        try
        {
            if (args.Length == 0)
                return Activator.CreateInstance(type);

            return Activator.CreateInstance(type, args);
        }
        catch (MissingMethodException)
        {
            throw new AlderException(DiagnosticDescriptors.NoMatchingConstructor, type.Name, args.Length);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    public static Type GetOpenValueTupleType(int arity) => arity switch
    {
        0 => typeof(ValueTuple),
        1 => typeof(ValueTuple<>),
        2 => typeof(ValueTuple<,>),
        3 => typeof(ValueTuple<,,>),
        4 => typeof(ValueTuple<,,,>),
        5 => typeof(ValueTuple<,,,,>),
        6 => typeof(ValueTuple<,,,,,>),
        7 => typeof(ValueTuple<,,,,,,>),
        8 => typeof(ValueTuple<,,,,,,,>),
        _ => throw new AlderException(DiagnosticDescriptors.UnsupportedTupleArity, arity)
    };

    public static object CreateTuple(object?[] elements)
    {
        if (elements.Length == 0)
            throw new AlderException(DiagnosticDescriptors.TupleTooFewElements);

        if (elements.Length <= 7)
        {
            var types = new Type[elements.Length];
            for (var i = 0; i < elements.Length; i++)
                types[i] = elements[i]?.GetType() ?? typeof(object);

            var tupleType = RuntimeGenericFactory.CloseGenericType(GetOpenValueTupleType(elements.Length), types);
            return Activator.CreateInstance(tupleType, elements)!;
        }

        var restElements = new object?[elements.Length - 7];
        Array.Copy(elements, 7, restElements, 0, restElements.Length);
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

        var nestedTupleType = RuntimeGenericFactory.CloseGenericType(typeof(ValueTuple<,,,,,,,>), genericArgs);
        return Activator.CreateInstance(nestedTupleType, ctorArgs)!;
    }

    public static object? DeconstructTuple(object? tupleValue, string[] variableNames, AlderContext context)
    {
        if (tupleValue is System.Runtime.CompilerServices.ITuple tuple)
        {
            if (tuple.Length != variableNames.Length)
                throw new AlderException(DiagnosticDescriptors.DeconstructionCountMismatch, variableNames.Length, tuple.Length);
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

        throw new AlderException(DiagnosticDescriptors.DeconstructionFailed, TypeNameFormatter.Of(tupleValue));
    }

    public static object?[]? TryDeconstruct(object value, int parameterCount)
    {
        var type = value.GetType();
        MethodInfo? match = null;

        foreach (var m in ReflectionRuntime.GetMethods(type, BindingFlags.Public | BindingFlags.Instance))
        {
            if (m.Name != "Deconstruct")
                continue;
            var parameters = m.GetParameters();
            if (parameters.Length != parameterCount)
                continue;
            var allOut = true;
            foreach (var p in parameters)
            {
                if (!p.IsOut) { allOut = false; break; }
            }
            if (allOut) { match = m; break; }
        }

        if (match == null)
            return null;

        var args = new object?[parameterCount];
        match.Invoke(value, args);
        return args;
    }

    public static object CreateTypedArray(Type elementType, object sizeValue)
    {
        var size = Convert.ToInt32(sizeValue);
        return RuntimeArrayFactory.Create(elementType, size);
    }

    public static object ConvertArrayToTyped(object sourceArrayObj, Type elementType)
    {
        var sourceArray = (Array)sourceArrayObj;
        var typedArray = RuntimeArrayFactory.Create(elementType, sourceArray.Length);
        for (var i = 0; i < sourceArray.Length; i++)
            typedArray.SetValue(sourceArray.GetValue(i), i);
        return typedArray;
    }

    public static object? ApplyPropertyInitializer(object obj, string propertyName, object? value, AlderOptions options, AlderContext context)
    {
        MemberAccess.SetMember(obj, propertyName, value, options, context);
        return obj;
    }

    public static Action<object, object?> ResolveCollectionAddInvoker(Type collectionType)
    {
        return _addInvokerCache.GetOrAdd(collectionType, static type =>
        {
            var method = ReflectionRuntime
                .GetMethods(type, BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(static m =>
                    string.Equals(m.Name, "Add", StringComparison.Ordinal) &&
                    m.GetParameters().Length == 1);

            if (method is null)
                throw new AlderException(DiagnosticDescriptors.MemberNotFound, type.Name, "Add");

            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var valueParam = Expression.Parameter(typeof(object), "value");
            var paramType = method.GetParameters()[0].ParameterType;

            var call = Expression.Call(
                Expression.Convert(instanceParam, type),
                method,
                paramType == typeof(object)
                    ? valueParam
                    : Expression.Convert(valueParam, paramType));

            return Expression.Lambda<Action<object, object?>>(call, instanceParam, valueParam).Compile();
        });
    }

    public static object? ApplyCollectionInitializer(object obj, object? value)
    {
        var addInvoker = ResolveCollectionAddInvoker(obj.GetType());
        addInvoker(obj, value);
        return obj;
    }

    public static object? ApplyIndexerInitializer(object obj, object key, object? value, AlderOptions options, AlderContext context)
    {
        MemberAccess.SetIndex(obj, key, value, options, context);
        return obj;
    }

    public static object CreateNamedTuple(object?[] elements, string?[] names)
    {
        var tuple = CreateTuple(elements);
        var nameMap = new Dictionary<string, int>();
        for (var i = 0; i < names.Length; i++)
        {
            if (names[i] is { } name)
                nameMap[name] = i;
        }
        return new NamedTupleValue(tuple, nameMap);
    }

    public static Range CreateSystemRange(object? start, object? end)
    {
        var startIndex = start switch
        {
            null => new Index(0, fromEnd: false),
            Index si => si,
            _ => new Index(Convert.ToInt32(start))
        };
        var endIndex = end switch
        {
            null => new Index(0, fromEnd: true),
            Index ei => ei,
            _ => new Index(Convert.ToInt32(end))
        };
        return new Range(startIndex, endIndex);
    }

    public static object CreateMultiDimArray(Type elementType, object[] sizes)
    {
        var intSizes = new int[sizes.Length];
        for (var i = 0; i < sizes.Length; i++)
            intSizes[i] = Convert.ToInt32(sizes[i]);
        return RuntimeArrayFactory.Create(elementType, intSizes);
    }

    public static object? MultiDimArrayGet(object arrayObj, object[] indices)
    {
        var targetType = arrayObj.GetType();
        if (arrayObj is not Array arr)
        {
            if (indices.Length > 1 && HasIndexerWithArity(targetType, indices.Length))
                throw new AlderException(DiagnosticDescriptors.MultiParameterIndexerNotSupported, targetType.Name);

            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, targetType.Name);
        }

        try
        {
            var intIndices = new int[indices.Length];
            for (var i = 0; i < indices.Length; i++)
                intIndices[i] = Convert.ToInt32(indices[i]);
            return arr.GetValue(intIndices);
        }
        catch (AlderException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IndexOutOfRangeException or InvalidCastException or ArgumentException or RankException)
        {
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, arr.GetType().Name);
        }
    }

    public static object? MultiDimArraySet(object arrayObj, object[] indices, object? value)
    {
        var targetType = arrayObj.GetType();
        if (arrayObj is not Array arr)
        {
            if (indices.Length > 1 && HasIndexerWithArity(targetType, indices.Length))
                throw new AlderException(DiagnosticDescriptors.MultiParameterIndexerNotSupported, targetType.Name);

            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, targetType.Name);
        }

        try
        {
            var intIndices = new int[indices.Length];
            for (var i = 0; i < indices.Length; i++)
                intIndices[i] = Convert.ToInt32(indices[i]);
            arr.SetValue(value, intIndices);
            return value;
        }
        catch (AlderException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IndexOutOfRangeException or InvalidCastException or ArgumentException or RankException)
        {
            throw new AlderException(DiagnosticDescriptors.BadIndexerAccess, arr.GetType().Name);
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
        var properties = ReflectionRuntime.GetProperties(targetType, BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            if (property.Name == "Item" && property.GetIndexParameters().Length == parameterCount)
                return true;
        }

        return false;
    }
}
