// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator

using CsEval.Interpretation;

namespace CsEval.Runtime;

/// <summary>
/// LINQ method dispatch and handlers.
/// </summary>
public static class LinqDispatcher
{
    internal delegate (bool Success, object? Value) LinqHandler(
        List<object?> list,
        object?[] args,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken cancellationToken);

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

        var list = enumerable.Cast<object?>().ToList();
        return handler(list, args, context, options, cancellationToken);
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
                handler = (list, a, ctx, opts, ct) => extHandler(list, a, ctx, opts, ct);
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

    internal static (bool, object?) HandleWhere(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [LambdaValue predicate]) return (false, null);
        return (true, list.Where(item => TypeHelpers.RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx, opts, ct))).ToList());
    }

    internal static (bool, object?) HandleSelect(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [LambdaValue selector]) return (false, null);
        return (true, list.Select(item => InvokeLambdaForLinq(selector, [item], ctx, opts, ct)).ToList());
    }

    internal static (bool, object?) HandleSelectMany(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [LambdaValue selector]) return (false, null);
        return (true, list.SelectMany(item =>
        {
            var result = InvokeLambdaForLinq(selector, [item], ctx, opts, ct);
            if (result is IEnumerable ie and not string)
                return ie.Cast<object?>();
            throw new CsEvalException("SelectMany selector must return an enumerable");
        }).ToList());
    }

    internal static (bool, object?) HandleFirst(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [LambdaValue predicate])
            return (true, list.First(item => TypeHelpers.RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, list.First());
    }

    internal static (bool, object?) HandleFirstOrDefault(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [LambdaValue predicate])
            return (true, list.FirstOrDefault(item => TypeHelpers.RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, list.FirstOrDefault());
    }

    internal static (bool, object?) HandleLast(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [LambdaValue predicate])
            return (true, list.Last(item => TypeHelpers.RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, list.Last());
    }

    internal static (bool, object?) HandleLastOrDefault(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [LambdaValue predicate])
            return (true, list.LastOrDefault(item => TypeHelpers.RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, list.LastOrDefault());
    }

    internal static (bool, object?) HandleSingle(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [LambdaValue predicate])
            return (true, list.Single(item => TypeHelpers.RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, list.Single());
    }

    internal static (bool, object?) HandleSingleOrDefault(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [LambdaValue predicate])
            return (true, list.SingleOrDefault(item => TypeHelpers.RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, list.SingleOrDefault());
    }

    internal static (bool, object?) HandleCount(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [LambdaValue predicate])
            return (true, list.Count(item => TypeHelpers.RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, list.Count);
    }

    internal static (bool, object?) HandleAny(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [LambdaValue predicate])
            return (true, list.Any(item => TypeHelpers.RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx, opts, ct))));
        return (true, list.Any());
    }

    internal static (bool, object?) HandleAll(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [LambdaValue predicate]) return (false, null);
        return (true, list.All(item => TypeHelpers.RequireBoolean(InvokeLambdaForLinq(predicate, [item], ctx, opts, ct))));
    }

    internal static (bool, object?) HandleSum(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (list.Count == 0)
            return (true, 0);

        var first = args is [LambdaValue sel]
            ? InvokeLambdaForLinq(sel, [list.FirstOrDefault(x => x != null) ?? list[0]], ctx, opts, ct)
            : list.FirstOrDefault(x => x != null) ?? list[0];

        var typeCode = first == null ? TypeCode.Empty : Type.GetTypeCode(first.GetType());
        if (typeCode is < TypeCode.SByte or > TypeCode.Decimal)
            throw new InvalidOperationException($"Sum() requires numeric elements, but found '{first?.GetType().Name ?? "null"}'");

        dynamic sum = first switch { decimal => 0m, double => 0.0, float => 0f, long => 0L, _ => 0 };

        if (args is [LambdaValue selector])
        {
            foreach (var item in list)
                sum += (dynamic)InvokeLambdaForLinq(selector, [item], ctx, opts, ct)!;
        }
        else
        {
            foreach (var item in list)
                sum += (dynamic)item!;
        }

        return (true, (object)sum);
    }

    internal static (bool, object?) HandleMin(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [LambdaValue selector])
            return (true, list.Min(item => InvokeLambdaForLinq(selector, [item], ctx, opts, ct)));
        return (true, list.Min());
    }

    internal static (bool, object?) HandleMax(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [LambdaValue selector])
            return (true, list.Max(item => InvokeLambdaForLinq(selector, [item], ctx, opts, ct)));
        return (true, list.Max());
    }

    internal static (bool, object?) HandleAverage(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");

        var first = args is [LambdaValue sel]
            ? InvokeLambdaForLinq(sel, [list.FirstOrDefault(x => x != null) ?? list[0]], ctx, opts, ct)
            : list.FirstOrDefault(x => x != null) ?? list[0];

        // .NET Average() returns: decimal for decimal, double for float, double for double, double for int/long
        var useDecimal = first is decimal;
        dynamic sum = first switch { decimal => 0m, double or float => 0.0, _ => 0.0 };

        if (args is [LambdaValue selector])
        {
            foreach (var item in list)
                sum += (dynamic)InvokeLambdaForLinq(selector, [item], ctx, opts, ct)!;
        }
        else
        {
            foreach (var item in list)
                sum += (dynamic)item!;
        }

        return (true, useDecimal ? sum / list.Count : sum / (double)list.Count);
    }

    internal static (bool, object?) HandleTake(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var countObj] || countObj is not int count) return (false, null);
        return (true, list.Take(count).ToList());
    }

    internal static (bool, object?) HandleSkip(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var countObj] || countObj is not int count) return (false, null);
        return (true, list.Skip(count).ToList());
    }

    internal static (bool, object?) HandleOrderBy(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [LambdaValue keySelector])
            return (true, list.OrderBy(item => InvokeLambdaForLinq(keySelector, [item], ctx, opts, ct)).ToList());
        return (true, list.OrderBy(x => x).ToList());
    }

    internal static (bool, object?) HandleOrderByDescending(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [LambdaValue keySelector])
            return (true, list.OrderByDescending(item => InvokeLambdaForLinq(keySelector, [item], ctx, opts, ct)).ToList());
        return (true, list.OrderByDescending(x => x).ToList());
    }

    internal static (bool, object?) HandleDistinct(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
        => (true, list.Distinct().ToList());

    internal static (bool, object?) HandleReverse(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        var result = new List<object?>(list);
        result.Reverse();
        return (true, result);
    }

    internal static (bool, object?) HandleToList(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
        => (true, list);

    internal static (bool, object?) HandleToArray(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
        => (true, list.ToArray());

    internal static (bool, object?) HandleConcat(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [IEnumerable other]) return (false, null);
        return (true, list.Concat(other.Cast<object?>()).ToList());
    }

    internal static (bool, object?) HandleExcept(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [IEnumerable other]) return (false, null);
        return (true, list.Except(other.Cast<object?>()).ToList());
    }

    internal static (bool, object?) HandleUnion(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [IEnumerable other]) return (false, null);
        return (true, list.Union(other.Cast<object?>()).ToList());
    }

    internal static (bool, object?) HandleIntersect(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [IEnumerable other]) return (false, null);
        return (true, list.Intersect(other.Cast<object?>()).ToList());
    }

    internal static (bool, object?) HandleZip(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [IEnumerable other and not string, LambdaValue selector])
            return (true, list.Zip(other.Cast<object?>(), (a, b) => InvokeLambdaForLinq(selector, [a, b], ctx, opts, ct)).ToList());

        if (args is [IEnumerable zipOther and not string])
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

    internal static (bool, object?) HandleContains(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var item]) return (false, null);
        return (true, list.Contains(item));
    }

    internal static (bool, object?) HandleSequenceEqual(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [IEnumerable other]) return (false, null);
        return (true, list.SequenceEqual(other.Cast<object?>()));
    }

    internal static (bool, object?) HandleAggregate(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [LambdaValue func])
            return (true, list.Aggregate((acc, item) => InvokeLambdaForLinq(func, [acc, item], ctx, opts, ct)));
        if (args is [var seed, LambdaValue func2])
            return (true, list.Aggregate(seed, (acc, item) => InvokeLambdaForLinq(func2, [acc, item], ctx, opts, ct)));
        return (false, null);
    }

    internal static (bool, object?) HandleGroupBy(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [LambdaValue keySelector]) return (false, null);
        var groups = list.GroupBy(item => InvokeLambdaForLinq(keySelector, [item], ctx, opts, ct));
        return (true, groups.Select(g => (object?)new Dictionary<string, object?>
        {
            ["Key"] = g.Key,
            ["Items"] = g.ToList()
        }).ToList());
    }

    internal static (bool, object?) HandleElementAt(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var indexObj]) return (false, null);
        var index = Convert.ToInt32(indexObj);
        return (true, list.ElementAt(index));
    }

    internal static (bool, object?) HandleElementAtOrDefault(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [var indexObj]) return (false, null);
        var index = Convert.ToInt32(indexObj);
        return (true, list.ElementAtOrDefault(index));
    }

    internal static (bool, object?) HandleDefaultIfEmpty(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var defaultValue])
            return (true, list.DefaultIfEmpty(defaultValue).ToList());
        return (true, list.DefaultIfEmpty().ToList());
    }

    internal static (bool, object?) HandleOfType(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
        => (true, list.Where(x => x != null).ToList());

    internal static (bool, object?) HandleCast(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
        => (true, list);

    internal static (bool, object?) HandleMinBy(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [LambdaValue selector]) return (false, null);
        if (list.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");
        return (true, list.MinBy(item => InvokeLambdaForLinq(selector, [item], ctx, opts, ct)));
    }

    internal static (bool, object?) HandleMaxBy(List<object?> list, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is not [LambdaValue selector]) return (false, null);
        if (list.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");
        return (true, list.MaxBy(item => InvokeLambdaForLinq(selector, [item], ctx, opts, ct)));
    }
}
