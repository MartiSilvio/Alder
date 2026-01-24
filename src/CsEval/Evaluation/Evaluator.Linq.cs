namespace CsEval.Evaluation;

public sealed partial class Evaluator
{
    /// <summary>
    /// Handler delegate for LINQ methods.
    /// </summary>
    private delegate (bool Success, object? Value) LinqMethodHandler(
        Evaluator evaluator, List<object?> list, object?[] args);

    /// <summary>
    /// Registry of LINQ method handlers. Aliases point to the same handler.
    /// </summary>
    private static readonly Dictionary<string, LinqMethodHandler> LinqMethodHandlers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Filtering
            ["where"] = HandleWhere,
            ["filter"] = HandleWhere, // JS alias

            // Projection
            ["select"] = HandleSelect,
            ["map"] = HandleSelect, // JS alias
            ["selectmany"] = HandleSelectMany,
            ["flatmap"] = HandleSelectMany, // JS alias

            // Element access
            ["first"] = HandleFirst,
            ["firstordefault"] = HandleFirstOrDefault,
            ["find"] = HandleFirstOrDefault, // JS alias
            ["last"] = HandleLast,
            ["lastordefault"] = HandleLastOrDefault,
            ["single"] = HandleSingle,
            ["singleordefault"] = HandleSingleOrDefault,

            // Quantifiers
            ["any"] = HandleAny,
            ["some"] = HandleAny, // JS alias
            ["all"] = HandleAll,
            ["every"] = HandleAll, // JS alias

            // Aggregation
            ["count"] = HandleCount,
            ["sum"] = HandleSum,
            ["average"] = HandleAverage,
            ["min"] = HandleMin,
            ["max"] = HandleMax,
            ["minby"] = HandleMinBy,
            ["maxby"] = HandleMaxBy,
            ["aggregate"] = HandleAggregate,
            ["reduce"] = HandleReduce, // JS style: reduce(fn) or reduce(fn, seed)

            // Ordering
            ["orderby"] = HandleOrderBy,
            ["orderbydescending"] = HandleOrderByDescending,
            ["reverse"] = HandleReverse,

            // Grouping
            ["groupby"] = HandleGroupBy,

            // Combining
            ["zip"] = HandleZip,
            ["concat"] = HandleConcat,

            // Set operations
            ["except"] = HandleExcept,
            ["intersect"] = HandleIntersect,
            ["union"] = HandleUnion,

            // Partitioning
            ["take"] = HandleTake,
            ["skip"] = HandleSkip,

            // Other
            ["distinct"] = HandleDistinct,
            ["contains"] = HandleContains,
            ["includes"] = HandleContains, // JS alias
            ["tolist"] = HandleToList,
            ["toarray"] = HandleToArray,
        };

    private static bool IsEnumerableMethod(string name) => LinqMethodHandlers.ContainsKey(name);

    private (bool Success, object? Value) TryInvokeEnumerableMethod(IEnumerable enumerable, string methodName,
        object?[] args)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (!LinqMethodHandlers.TryGetValue(methodName, out var handler))
            return (false, null);

        var list = enumerable.Cast<object?>().ToList();
        return handler(this, list, args);
    }

    #region Handler Implementations

    private static (bool, object?) HandleWhere(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is not [LambdaValue predicate]) return (false, null);
        return (true, list.Where(item => IsTruthy(e.InvokeLambda(predicate, [item]))).ToList());
    }

    private static (bool, object?) HandleSelect(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is not [LambdaValue selector]) return (false, null);
        return (true, list.Select(item => e.InvokeLambda(selector, [item])).ToList());
    }

    private static (bool, object?) HandleSelectMany(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is not [LambdaValue selector]) return (false, null);
        return (true, list.SelectMany(item =>
        {
            var result = e.InvokeLambda(selector, [item]);
            if (result is IEnumerable innerEnumerable and not string)
                return innerEnumerable.Cast<object?>();
            throw new EvalException("SelectMany selector must return an enumerable");
        }).ToList());
    }

    private static (bool, object?) HandleFirst(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is [LambdaValue predicate])
            return (true, list.First(item => IsTruthy(e.InvokeLambda(predicate, [item]))));
        return (true, list.First());
    }

    private static (bool, object?) HandleFirstOrDefault(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is [LambdaValue predicate])
            return (true, list.FirstOrDefault(item => IsTruthy(e.InvokeLambda(predicate, [item]))));
        return (true, list.FirstOrDefault());
    }

    private static (bool, object?) HandleLast(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is [LambdaValue predicate])
            return (true, list.Last(item => IsTruthy(e.InvokeLambda(predicate, [item]))));
        return (true, list.Last());
    }

    private static (bool, object?) HandleLastOrDefault(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is [LambdaValue predicate])
            return (true, list.LastOrDefault(item => IsTruthy(e.InvokeLambda(predicate, [item]))));
        return (true, list.LastOrDefault());
    }

    private static (bool, object?) HandleSingle(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is [LambdaValue predicate])
            return (true, list.Single(item => IsTruthy(e.InvokeLambda(predicate, [item]))));
        return (true, list.Single());
    }

    private static (bool, object?) HandleSingleOrDefault(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is [LambdaValue predicate])
            return (true, list.SingleOrDefault(item => IsTruthy(e.InvokeLambda(predicate, [item]))));
        return (true, list.SingleOrDefault());
    }

    private static (bool, object?) HandleAny(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is [LambdaValue predicate])
            return (true, list.Any(item => IsTruthy(e.InvokeLambda(predicate, [item]))));
        return (true, list.Any());
    }

    private static (bool, object?) HandleAll(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is not [LambdaValue predicate]) return (false, null);
        return (true, list.All(item => IsTruthy(e.InvokeLambda(predicate, [item]))));
    }

    private static (bool, object?) HandleCount(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is [LambdaValue predicate])
            return (true, list.Count(item => IsTruthy(e.InvokeLambda(predicate, [item]))));
        return (true, list.Count);
    }

    private static (bool, object?) HandleSum(Evaluator e, List<object?> list, object?[] args)
    {
        if (list.Count == 0)
            return (true, 0);

        // Get first element to determine initial accumulator type
        var first = args is [LambdaValue sel]
            ? e.InvokeLambda(sel, [list.FirstOrDefault(x => x != null) ?? list[0]])
            : list.FirstOrDefault(x => x != null) ?? list[0];

        // Initialize with typed zero, let C# handle arithmetic via dynamic
        dynamic sum = first switch { decimal => 0m, double => 0.0, float => 0f, long => 0L, _ => 0 };

        if (args is [LambdaValue selector])
        {
            foreach (var item in list)
                sum += (dynamic)e.InvokeLambda(selector, [item])!;
        }
        else
        {
            foreach (var item in list)
                sum += (dynamic)item!;
        }

        return (true, (object)sum);
    }

    private static (bool, object?) HandleAverage(Evaluator e, List<object?> list, object?[] args)
    {
        if (list.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");

        // Get first element to determine type
        var first = args is [LambdaValue sel]
            ? e.InvokeLambda(sel, [list.FirstOrDefault(x => x != null) ?? list[0]])
            : list.FirstOrDefault(x => x != null) ?? list[0];

        // C# Average: decimal → decimal, everything else → double
        if (first is decimal)
        {
            if (args is [LambdaValue selector])
                return (true, list.Average(item => Convert.ToDecimal(e.InvokeLambda(selector, [item]))));
            return (true, list.Average(item => Convert.ToDecimal(item)));
        }

        if (args is [LambdaValue sel2])
            return (true, list.Average(item => Convert.ToDouble(e.InvokeLambda(sel2, [item]))));
        return (true, list.Average(item => Convert.ToDouble(item)));
    }

    private static (bool, object?) HandleMin(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is [LambdaValue selector])
            return (true, list.Min(item => e.InvokeLambda(selector, [item])));
        return (true, list.Min());
    }

    private static (bool, object?) HandleMax(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is [LambdaValue selector])
            return (true, list.Max(item => e.InvokeLambda(selector, [item])));
        return (true, list.Max());
    }

    private static (bool, object?) HandleAggregate(Evaluator e, List<object?> list, object?[] args)
    {
        // C# style with seed: aggregate(seed, (acc, item) => ...)
        if (args is [var seed, LambdaValue aggregator])
            return (true, list.Aggregate(seed, (acc, item) => e.InvokeLambda(aggregator, [acc, item])));

        // Without seed: aggregate((acc, item) => ...) - uses first element as seed
        if (args is [LambdaValue reducer])
            return (true, list.Skip(1).Aggregate(list.FirstOrDefault(), (acc, item) => e.InvokeLambda(reducer, [acc, item])));

        return (false, null);
    }

    private static (bool, object?) HandleReduce(Evaluator e, List<object?> list, object?[] args)
    {
        // JS style with seed: reduce((acc, item) => ..., seed)
        if (args is [LambdaValue reducer, var seed])
            return (true, list.Aggregate(seed, (acc, item) => e.InvokeLambda(reducer, [acc, item])));

        // Without seed: reduce((acc, item) => ...) - uses first element as seed
        if (args is [LambdaValue reducerOnly])
            return (true, list.Skip(1).Aggregate(list.FirstOrDefault(), (acc, item) => e.InvokeLambda(reducerOnly, [acc, item])));

        return (false, null);
    }

    private static (bool, object?) HandleOrderBy(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is not [LambdaValue selector]) return (false, null);
        return (true, list.OrderBy(item => e.InvokeLambda(selector, [item])).ToList());
    }

    private static (bool, object?) HandleOrderByDescending(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is not [LambdaValue selector]) return (false, null);
        return (true, list.OrderByDescending(item => e.InvokeLambda(selector, [item])).ToList());
    }

    private static (bool, object?) HandleReverse(Evaluator e, List<object?> list, object?[] args)
    {
        var reversed = list.ToList();
        reversed.Reverse();
        return (true, reversed);
    }

    private static (bool, object?) HandleGroupBy(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is not [LambdaValue keySelector]) return (false, null);
        var groups = list.GroupBy(item => e.InvokeLambda(keySelector, [item]));
        return (true, groups.Select(g => (object?)new Dictionary<string, object?>
        {
            ["Key"] = g.Key,
            ["Items"] = g.ToList()
        }).ToList());
    }

    private static (bool, object?) HandleZip(Evaluator e, List<object?> list, object?[] args)
    {
        // With selector: zip(other, (a, b) => ...)
        if (args is [IEnumerable other and not string, LambdaValue selector])
        {
            var otherList = other.Cast<object?>().ToList();
            return (true, list.Zip(otherList, (first, second) => e.InvokeLambda(selector, [first, second])).ToList());
        }

        // Without selector: zip(other) - returns { First, Second } dictionaries
        if (args is [IEnumerable zipOther and not string])
        {
            var otherList = zipOther.Cast<object?>().ToList();
            return (true, list.Zip(otherList, (first, second) => (object?)new Dictionary<string, object?>
            {
                ["First"] = first,
                ["Second"] = second
            }).ToList());
        }

        if (args.Length >= 1)
            throw new EvalException($"Zip requires an enumerable argument, got {args[0]?.GetType().Name ?? "null"}");

        return (false, null);
    }

    private static (bool, object?) HandleConcat(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is not [IEnumerable other and not string])
        {
            if (args.Length == 1)
                throw new EvalException($"Concat requires an enumerable argument, got {args[0]?.GetType().Name ?? "null"}");
            return (false, null);
        }
        return (true, list.Concat(other.Cast<object?>()).ToList());
    }

    private static (bool, object?) HandleTake(Evaluator e, List<object?> list, object?[] args)
    {
        if (args.Length != 1) return (false, null);
        return (true, list.Take(Convert.ToInt32(args[0])).ToList());
    }

    private static (bool, object?) HandleSkip(Evaluator e, List<object?> list, object?[] args)
    {
        if (args.Length != 1) return (false, null);
        return (true, list.Skip(Convert.ToInt32(args[0])).ToList());
    }

    private static (bool, object?) HandleDistinct(Evaluator e, List<object?> list, object?[] args)
    {
        return (true, list.Distinct().ToList());
    }

    private static (bool, object?) HandleContains(Evaluator e, List<object?> list, object?[] args)
    {
        if (args.Length != 1) return (false, null);

        var searchValue = args[0];
        foreach (var item in list)
        {
            if (item is null && searchValue is null)
                return (true, true);
            if (item is null || searchValue is null)
                continue;
            if (IsNumeric(item) && IsNumeric(searchValue))
            {
                // C# forbids decimal == float/double, so handle that case specially
                if (InvolvesDecimalAndFloatingPoint(item, searchValue))
                {
                    if (Convert.ToDouble(item) == Convert.ToDouble(searchValue))
                        return (true, true);
                }
                // Use dynamic to let C# runtime handle all other numeric comparisons
                else if ((dynamic)item == (dynamic)searchValue)
                    return (true, true);
            }
            else if (item.Equals(searchValue))
            {
                return (true, true);
            }
        }
        return (true, false);
    }

    private static (bool, object?) HandleToList(Evaluator e, List<object?> list, object?[] args)
    {
        return (true, list);
    }

    private static (bool, object?) HandleToArray(Evaluator e, List<object?> list, object?[] args)
    {
        return (true, list.ToArray());
    }

    private static (bool, object?) HandleMinBy(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is not [LambdaValue selector]) return (false, null);
        if (list.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");
        return (true, list.MinBy(item => e.InvokeLambda(selector, [item])));
    }

    private static (bool, object?) HandleMaxBy(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is not [LambdaValue selector]) return (false, null);
        if (list.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");
        return (true, list.MaxBy(item => e.InvokeLambda(selector, [item])));
    }

    private static (bool, object?) HandleExcept(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is not [IEnumerable other and not string])
        {
            if (args.Length == 1)
                throw new EvalException($"Except requires an enumerable argument, got {args[0]?.GetType().Name ?? "null"}");
            return (false, null);
        }
        return (true, list.Except(other.Cast<object?>()).ToList());
    }

    private static (bool, object?) HandleIntersect(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is not [IEnumerable other and not string])
        {
            if (args.Length == 1)
                throw new EvalException($"Intersect requires an enumerable argument, got {args[0]?.GetType().Name ?? "null"}");
            return (false, null);
        }
        return (true, list.Intersect(other.Cast<object?>()).ToList());
    }

    private static (bool, object?) HandleUnion(Evaluator e, List<object?> list, object?[] args)
    {
        if (args is not [IEnumerable other and not string])
        {
            if (args.Length == 1)
                throw new EvalException($"Union requires an enumerable argument, got {args[0]?.GetType().Name ?? "null"}");
            return (false, null);
        }
        return (true, list.Union(other.Cast<object?>()).ToList());
    }

    /// <summary>
    /// Checks if one value is decimal and the other is float/double.
    /// C# forbids mixing these types in arithmetic and comparison.
    /// </summary>
    private static bool InvolvesDecimalAndFloatingPoint(object? a, object? b)
    {
        var aIsDecimal = a is decimal;
        var bIsDecimal = b is decimal;
        var aIsFloatingPoint = a is float or double;
        var bIsFloatingPoint = b is float or double;
        return (aIsDecimal && bIsFloatingPoint) || (bIsDecimal && aIsFloatingPoint);
    }

    #endregion
}
