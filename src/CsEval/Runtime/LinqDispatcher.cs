// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator

using CsEval.Interpretation;

namespace CsEval.Runtime;

/// <summary>
/// LINQ method dispatch and handlers.
/// </summary>
public static class LinqDispatcher
{
    /// <summary>
    /// Delegate for LINQ method handlers.
    /// </summary>
    public delegate (bool Success, object? Value) LinqHandler(
        IEnumerable<object?> source,
        Type elementType,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken cancellationToken);

    private static Type GetElementType(IEnumerable enumerable)
    {
        var type = enumerable.GetType();

        // Check for IEnumerable<T> interface (handles iterators like Enumerable.Range)
        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        }

        if (type.IsGenericType)
        {
            var args = type.GetGenericArguments();
            if (args.Length > 0)
                return args[0];
        }
        if (type.IsArray)
            return type.GetElementType() ?? typeof(object);
        return typeof(object);
    }

    private static object CreateTypedList(IEnumerable<object?> items, Type elementType)
    {
        if (elementType == typeof(object))
            return items.ToList();

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
        foreach (var item in items)
            list.Add(item);
        return list;
    }

    private static readonly Dictionary<string, LinqHandler> CoreHandlers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Filtering
            ["Where"] = HandleWhere,

            // Projection
            ["Select"] = HandleSelect,
            ["SelectMany"] = HandleSelectMany,

            // Element access
            ["First"] = HandleFirst,
            ["FirstOrDefault"] = HandleFirstOrDefault,
            ["Last"] = HandleLast,
            ["LastOrDefault"] = HandleLastOrDefault,
            ["Single"] = HandleSingle,
            ["SingleOrDefault"] = HandleSingleOrDefault,
            ["ElementAt"] = HandleElementAt,
            ["ElementAtOrDefault"] = HandleElementAtOrDefault,

            // Quantifiers
            ["Any"] = HandleAny,
            ["All"] = HandleAll,

            // Aggregation
            ["Count"] = HandleCount,
            ["Sum"] = HandleSum,
            ["Average"] = HandleAverage,
            ["Min"] = HandleMin,
            ["Max"] = HandleMax,
            ["MinBy"] = HandleMinBy,
            ["MaxBy"] = HandleMaxBy,
            ["Aggregate"] = HandleAggregate,

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
            ["SequenceEqual"] = HandleSequenceEqual,
            ["DefaultIfEmpty"] = HandleDefaultIfEmpty,
            ["ToList"] = HandleToList,
            ["ToArray"] = HandleToArray,
            ["OfType"] = HandleOfType,
            ["Cast"] = HandleCast,
        };

    internal static bool IsLinqMethod(string methodName, CsEvalOptions options)
    {
        if (CoreHandlers.ContainsKey(methodName))
            return true;

        return options.Extensions.Any(ext => ext.LinqHandlers.ContainsKey(methodName));
    }

    internal static (bool Success, object? Value) TryInvokeEnumerableMethod(
        IEnumerable enumerable,
        string methodName,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetHandler(methodName, options, out var handler))
            return (false, null);

        var elementType = GetElementType(enumerable);
        return handler(enumerable.Cast<object?>(), elementType, args, context, options, cancellationToken);
    }

    private static bool TryGetHandler(
        string methodName,
        CsEvalOptions options,
        out LinqHandler handler)
    {
        if (CoreHandlers.TryGetValue(methodName, out handler!))
            return true;

        foreach (var ext in options.Extensions)
        {
            if (ext.LinqHandlers.TryGetValue(methodName, out var extHandler))
            {
                handler = extHandler;
                return true;
            }
        }

        handler = null!;
        return false;
    }

    internal static object? InvokeLambdaForLinq(
        LambdaValue lambda,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken cancellationToken)
    {
        var childContext = lambda.Closure.CreateChild();
        for (var i = 0; i < lambda.Parameters.Count && i < args.Length; i++)
            childContext.Define(lambda.Parameters[i], args[i]);
        var evaluator = new Evaluator(childContext, new Dictionary<string, Func<object?[], object?>>(), options, cancellationToken);
        return evaluator.Evaluate(lambda.Body);
    }

    internal static object? InvokeCompiledLambdaForLinq(
        CompiledLambdaValue lambda,
        object?[] args)
    {
        return lambda.CompiledBody(args, lambda.Closure);
    }

    internal static object? InvokeAnyLambdaForLinq(
        object? lambda,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken cancellationToken)
    {
        return lambda switch
        {
            CompiledLambdaValue compiled => InvokeCompiledLambdaForLinq(compiled, args),
            LambdaValue interpreted => InvokeLambdaForLinq(interpreted, args, context, options, cancellationToken),
            _ => throw new CsEvalException($"Expected lambda but got {lambda?.GetType().Name ?? "null"}")
        };
    }

    internal static bool IsLambda(object? value) => value is LambdaValue or CompiledLambdaValue;

    /// <summary>
    /// Backward-compatible overload for extension handlers that don't have access to options/cancellation.
    /// </summary>
    internal static object? InvokeLambdaForLinq(
        LambdaValue lambda,
        object?[] args,
        CsEvalContext context)
    {
        return InvokeLambdaForLinq(lambda, args, context, CsEvalOptions.Default, CancellationToken.None);
    }

    internal static (bool, object?) HandleWhere(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var predicate] || !IsLambda(predicate)) return (false, null);
        return (true, source.Where(item => TypeHelpers.RequireBoolean(InvokeAnyLambdaForLinq(predicate, [item], ctx, opts, ct))));
    }

    internal static (bool, object?) HandleSelect(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var selector] || !IsLambda(selector)) return (false, null);
        return (true, source.Select(item => InvokeAnyLambdaForLinq(selector, [item], ctx, opts, ct)));
    }

    internal static (bool, object?) HandleSelectMany(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var selector] || !IsLambda(selector)) return (false, null);
        return (true, source.SelectMany(item =>
        {
            var result = InvokeAnyLambdaForLinq(selector, [item], ctx, opts, ct);
            if (result is IEnumerable ie and not string)
                return ie.Cast<object?>();
            throw new CsEvalException("SelectMany selector must return an enumerable");
        }));
    }

    internal static (bool, object?) HandleFirst(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var predicate] && IsLambda(predicate))
            return (true, source.First(item => TypeHelpers.RequireBoolean(InvokeAnyLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, source.First());
    }

    internal static (bool, object?) HandleFirstOrDefault(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var predicate] && IsLambda(predicate))
            return (true, source.FirstOrDefault(item => TypeHelpers.RequireBoolean(InvokeAnyLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, source.FirstOrDefault());
    }

    internal static (bool, object?) HandleLast(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var predicate] && IsLambda(predicate))
            return (true, source.Last(item => TypeHelpers.RequireBoolean(InvokeAnyLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, source.Last());
    }

    internal static (bool, object?) HandleLastOrDefault(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var predicate] && IsLambda(predicate))
            return (true, source.LastOrDefault(item => TypeHelpers.RequireBoolean(InvokeAnyLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, source.LastOrDefault());
    }

    internal static (bool, object?) HandleSingle(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var predicate] && IsLambda(predicate))
            return (true, source.Single(item => TypeHelpers.RequireBoolean(InvokeAnyLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, source.Single());
    }

    internal static (bool, object?) HandleSingleOrDefault(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var predicate] && IsLambda(predicate))
            return (true, source.SingleOrDefault(item => TypeHelpers.RequireBoolean(InvokeAnyLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, source.SingleOrDefault());
    }

    internal static (bool, object?) HandleCount(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var predicate] && IsLambda(predicate))
            return (true, source.Count(item => TypeHelpers.RequireBoolean(InvokeAnyLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, source.Count());
    }

    internal static (bool, object?) HandleAny(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var predicate] && IsLambda(predicate))
            return (true, source.Any(item => TypeHelpers.RequireBoolean(InvokeAnyLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, source.Any());
    }

    internal static (bool, object?) HandleAll(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var predicate] || !IsLambda(predicate)) return (false, null);
        return (true, source.All(item => TypeHelpers.RequireBoolean(InvokeAnyLambdaForLinq(predicate, [item], ctx, opts, ct))));
    }

    internal static (bool, object?) HandleSum(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        var list = source.ToList();
        if (list.Count == 0)
            return (true, 0);

        var first = args is [var sel] && IsLambda(sel)
            ? InvokeAnyLambdaForLinq(sel, [list.FirstOrDefault(x => x != null) ?? list[0]], ctx, opts, ct)
            : list.FirstOrDefault(x => x != null) ?? list[0];

        var typeCode = first == null ? TypeCode.Empty : Type.GetTypeCode(first.GetType());
        if (typeCode is < TypeCode.SByte or > TypeCode.Decimal)
            throw new InvalidOperationException($"Sum() requires numeric elements, but found '{first?.GetType().Name ?? "null"}'");

        dynamic sum = first switch { decimal => 0m, double => 0.0, float => 0f, long => 0L, _ => 0 };

        if (args is [var selector] && IsLambda(selector))
        {
            foreach (var item in list)
                sum += (dynamic)InvokeAnyLambdaForLinq(selector, [item], ctx, opts, ct)!;
        }
        else
        {
            foreach (var item in list)
                sum += (dynamic)item!;
        }

        return (true, (object)sum);
    }

    internal static (bool, object?) HandleMin(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var selector] && IsLambda(selector))
            return (true, source.Min(item => InvokeAnyLambdaForLinq(selector, [item], ctx, opts, ct)));
        return (true, source.Min());
    }

    internal static (bool, object?) HandleMax(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var selector] && IsLambda(selector))
            return (true, source.Max(item => InvokeAnyLambdaForLinq(selector, [item], ctx, opts, ct)));
        return (true, source.Max());
    }

    internal static (bool, object?) HandleAverage(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        var list = source.ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");

        var first = args is [var sel] && IsLambda(sel)
            ? InvokeAnyLambdaForLinq(sel, [list.FirstOrDefault(x => x != null) ?? list[0]], ctx, opts, ct)
            : list.FirstOrDefault(x => x != null) ?? list[0];

        var useDecimal = first is decimal;
        dynamic sum = first switch { decimal => 0m, double or float => 0.0, _ => 0.0 };

        if (args is [var selector] && IsLambda(selector))
        {
            foreach (var item in list)
                sum += (dynamic)InvokeAnyLambdaForLinq(selector, [item], ctx, opts, ct)!;
        }
        else
        {
            foreach (var item in list)
                sum += (dynamic)item!;
        }

        return (true, useDecimal ? sum / list.Count : sum / (double)list.Count);
    }

    internal static (bool, object?) HandleTake(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var countObj] || countObj is not int count) return (false, null);
        return (true, source.Take(count));
    }

    internal static (bool, object?) HandleSkip(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var countObj] || countObj is not int count) return (false, null);
        return (true, source.Skip(count));
    }

    internal static (bool, object?) HandleOrderBy(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var keySelector] && IsLambda(keySelector))
            return (true, source.OrderBy(item => InvokeAnyLambdaForLinq(keySelector, [item], ctx, opts, ct)));
        return (true, source.OrderBy(x => x));
    }

    internal static (bool, object?) HandleOrderByDescending(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var keySelector] && IsLambda(keySelector))
            return (true, source.OrderByDescending(item => InvokeAnyLambdaForLinq(keySelector, [item], ctx, opts, ct)));
        return (true, source.OrderByDescending(x => x));
    }

    internal static (bool, object?) HandleDistinct(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
        => (true, source.Distinct());

    internal static (bool, object?) HandleReverse(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
        => (true, source.Reverse());

    internal static (bool, object?) HandleToList(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
        => (true, RuntimeHelpers.CreateTypedList(source.ToList()));

    internal static (bool, object?) HandleToArray(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        var list = source.ToList();
        var typedList = RuntimeHelpers.CreateTypedList(list);
        if (typedList is Array arr)
            return (true, arr);
        if (typedList is System.Collections.IList typedIList)
        {
            var listElementType = typedList.GetType().GetGenericArguments().FirstOrDefault() ?? typeof(object);
            var array = Array.CreateInstance(listElementType, typedIList.Count);
            for (var i = 0; i < typedIList.Count; i++)
                array.SetValue(typedIList[i], i);
            return (true, array);
        }
        return (true, list.ToArray());
    }

    internal static (bool, object?) HandleConcat(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [IEnumerable other]) return (false, null);
        return (true, source.Concat(other.Cast<object?>()));
    }

    internal static (bool, object?) HandleExcept(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [IEnumerable other]) return (false, null);
        return (true, source.Except(other.Cast<object?>()));
    }

    internal static (bool, object?) HandleUnion(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [IEnumerable other]) return (false, null);
        return (true, source.Union(other.Cast<object?>()));
    }

    internal static (bool, object?) HandleIntersect(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [IEnumerable other]) return (false, null);
        return (true, source.Intersect(other.Cast<object?>()));
    }

    internal static (bool, object?) HandleZip(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [IEnumerable other and not string, var selector] && IsLambda(selector))
        {
            return (true, source.Zip(other.Cast<object?>(), (a, b) => InvokeAnyLambdaForLinq(selector, [a, b], ctx, opts, ct)));
        }

        if (args is [IEnumerable zipOther and not string])
        {
            return (true, source.Zip(zipOther.Cast<object?>(), (first, second) => (object?)new Dictionary<string, object?>
            {
                ["First"] = first,
                ["Second"] = second
            }));
        }

        return (false, null);
    }

    internal static (bool, object?) HandleContains(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var item]) return (false, null);
        return (true, source.Contains(item));
    }

    internal static (bool, object?) HandleSequenceEqual(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [IEnumerable other]) return (false, null);
        return (true, source.SequenceEqual(other.Cast<object?>()));
    }

    internal static (bool, object?) HandleAggregate(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var func] && IsLambda(func))
            return (true, source.Aggregate((acc, item) => InvokeAnyLambdaForLinq(func, [acc, item], ctx, opts, ct)));
        if (args is [var seed, var func2] && IsLambda(func2))
            return (true, source.Aggregate(seed, (acc, item) => InvokeAnyLambdaForLinq(func2, [acc, item], ctx, opts, ct)));
        return (false, null);
    }

    internal static (bool, object?) HandleGroupBy(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var keySelector] || !IsLambda(keySelector)) return (false, null);
        var groups = source.GroupBy(item => InvokeAnyLambdaForLinq(keySelector, [item], ctx, opts, ct));
        return (true, groups.Select(g => (object?)new Dictionary<string, object?>
        {
            ["Key"] = g.Key,
            ["Items"] = CreateTypedList(g, elementType)
        }));
    }

    internal static (bool, object?) HandleElementAt(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var indexObj]) return (false, null);
        var index = Convert.ToInt32(indexObj);
        return (true, source.ElementAt(index));
    }

    internal static (bool, object?) HandleElementAtOrDefault(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var indexObj]) return (false, null);
        var index = Convert.ToInt32(indexObj);
        return (true, source.ElementAtOrDefault(index));
    }

    internal static (bool, object?) HandleDefaultIfEmpty(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var defaultValue])
            return (true, source.DefaultIfEmpty(defaultValue));
        return (true, source.DefaultIfEmpty());
    }

    internal static (bool, object?) HandleOfType(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
        => (true, source.Where(x => x != null));

    internal static (bool, object?) HandleCast(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
        => (true, source);

    internal static (bool, object?) HandleMinBy(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var selector] || !IsLambda(selector)) return (false, null);
        var list = source.ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");
        return (true, list.MinBy(item => InvokeAnyLambdaForLinq(selector, [item], ctx, opts, ct)));
    }

    internal static (bool, object?) HandleMaxBy(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var selector] || !IsLambda(selector)) return (false, null);
        var list = source.ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");
        return (true, list.MaxBy(item => InvokeAnyLambdaForLinq(selector, [item], ctx, opts, ct)));
    }
}
