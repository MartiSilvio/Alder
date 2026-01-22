namespace CsEval.Evaluation;

public sealed partial class Evaluator
{
    private static bool IsEnumerableMethod(string name)
        => name.ToLowerInvariant() switch
        {
            "where" => true,
            "select" => true,
            "selectmany" => true,
            "aggregate" => true,
            "first" => true,
            "firstordefault" => true,
            "last" => true,
            "lastordefault" => true,
            "single" => true,
            "singleordefault" => true,
            "any" => true,
            "all" => true,
            "count" => true,
            "sum" => true,
            "average" => true,
            "min" => true,
            "max" => true,
            "orderby" => true,
            "orderbydescending" => true,
            "groupby" => true,
            "zip" => true,
            "distinct" => true,
            "take" => true,
            "skip" => true,
            "contains" => true,
            "reverse" => true,
            "tolist" => true,
            "toarray" => true,
            "concat" => true,
            _ => false
        };

    private (bool Success, object? Value) TryInvokeEnumerableMethod(IEnumerable enumerable, string methodName,
        object?[] args)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        var list = enumerable.Cast<object?>().ToList();

        switch (methodName.ToLowerInvariant())
        {
            case "where" when args is [LambdaValue predicate]:
                return (true, list.Where(item => IsTruthy(InvokeLambda(predicate, [item]))).ToList());

            case "select" when args is [LambdaValue selector]:
                return (true, list.Select(item => InvokeLambda(selector, [item])).ToList());

            case "aggregate" when args is [LambdaValue aggregator, _]:
                return (true, list.Aggregate(args[1], (acc, item) => InvokeLambda(aggregator, [acc, item])));

            case "aggregate" when args is [LambdaValue reducer]:
                return (true,
                    list.Skip(1).Aggregate(list.FirstOrDefault(), (acc, item) => InvokeLambda(reducer, [acc, item])));

            case "first":
                if (args is [LambdaValue firstPredicate])
                    return (true, list.First(item => IsTruthy(InvokeLambda(firstPredicate, [item]))));
                return (true, list.First());

            case "firstordefault":
                if (args is [LambdaValue firstOrDefaultPredicate])
                    return (true, list.FirstOrDefault(item => IsTruthy(InvokeLambda(firstOrDefaultPredicate, [item]))));
                return (true, list.FirstOrDefault());

            case "last":
                if (args is [LambdaValue lastPredicate])
                    return (true, list.Last(item => IsTruthy(InvokeLambda(lastPredicate, [item]))));
                return (true, list.Last());

            case "lastordefault":
                if (args is [LambdaValue lastOrDefaultPredicate])
                    return (true, list.LastOrDefault(item => IsTruthy(InvokeLambda(lastOrDefaultPredicate, [item]))));
                return (true, list.LastOrDefault());

            case "single":
                if (args is [LambdaValue singlePredicate])
                    return (true, list.Single(item => IsTruthy(InvokeLambda(singlePredicate, [item]))));
                return (true, list.Single());

            case "singleordefault":
                if (args is [LambdaValue singleOrDefaultPredicate])
                    return (true,
                        list.SingleOrDefault(item => IsTruthy(InvokeLambda(singleOrDefaultPredicate, [item]))));
                return (true, list.SingleOrDefault());

            case "any":
                if (args is [LambdaValue anyPredicate])
                    return (true, list.Any(item => IsTruthy(InvokeLambda(anyPredicate, [item]))));
                return (true, list.Any());

            case "all" when args is [LambdaValue allPredicate]:
                return (true, list.All(item => IsTruthy(InvokeLambda(allPredicate, [item]))));

            case "count":
                if (args is [LambdaValue countPredicate])
                    return (true, list.Count(item => IsTruthy(InvokeLambda(countPredicate, [item]))));
                return (true, list.Count);

            case "sum":
                if (args is [LambdaValue sumSelector])
                    return (true, list.Sum(item => ToDouble(InvokeLambda(sumSelector, [item]))));
                return (true, list.Sum(item => ToDouble(item)));

            case "average":
                if (args is [LambdaValue avgSelector])
                    return (true, list.Average(item => ToDouble(InvokeLambda(avgSelector, [item]))));
                return (true, list.Average(item => ToDouble(item)));

            case "min":
                if (args is [LambdaValue minSelector])
                    return (true, list.Min(item => InvokeLambda(minSelector, [item])));
                return (true, list.Min());

            case "max":
                if (args is [LambdaValue maxSelector])
                    return (true, list.Max(item => InvokeLambda(maxSelector, [item])));
                return (true, list.Max());

            case "orderby" when args is [LambdaValue orderSelector]:
                return (true, list.OrderBy(item => InvokeLambda(orderSelector, [item])).ToList());

            case "orderbydescending" when args is [LambdaValue orderDescSelector]:
                return (true, list.OrderByDescending(item => InvokeLambda(orderDescSelector, [item])).ToList());

            case "distinct":
                return (true, list.Distinct().ToList());

            case "take" when args.Length == 1:
                return (true, list.Take(Convert.ToInt32(args[0])).ToList());

            case "skip" when args.Length == 1:
                return (true, list.Skip(Convert.ToInt32(args[0])).ToList());

            case "contains" when args.Length == 1:
                // Use numeric comparison when both values are numeric to handle type mismatches
                var searchValue = args[0];
                foreach (var item in list)
                {
                    if (item is null && searchValue is null)
                        return (true, true);
                    if (item is null || searchValue is null)
                        continue;
                    if (IsNumeric(item) && IsNumeric(searchValue))
                    {
                        if (ToDouble(item) == ToDouble(searchValue))
                            return (true, true);
                    }
                    else if (item.Equals(searchValue))
                    {
                        return (true, true);
                    }
                }
                return (true, false);

            case "reverse":
                var reversed = list.ToList();
                reversed.Reverse();
                return (true, reversed);

            case "tolist":
                return (true, list);

            case "toarray":
                return (true, list.ToArray());

            case "concat" when args.Length == 1:
                if (args[0] is IEnumerable second && args[0] is not string)
                    return (true, list.Concat(second.Cast<object?>()).ToList());
                throw new EvalException(
                    $"Concat requires an enumerable argument, got {args[0]?.GetType().Name ?? "null"}");

            case "selectmany" when args is [LambdaValue selectManySelector]:
                return (true, list.SelectMany(item =>
                {
                    var result = InvokeLambda(selectManySelector, [item]);
                    if (result is IEnumerable innerEnumerable and not string)
                        return innerEnumerable.Cast<object?>();
                    throw new EvalException("SelectMany selector must return an enumerable");
                }).ToList());

            case "groupby" when args is [LambdaValue groupKeySelector]:
                var groups = list.GroupBy(item => InvokeLambda(groupKeySelector, [item]));
                // Return as list of dictionaries with Key and Items, cast to List<object?> for consistency
                return (true, groups.Select(g => (object?)new Dictionary<string, object?>
                {
                    ["Key"] = g.Key,
                    ["Items"] = g.ToList()
                }).ToList());

            case "zip" when args is [_, LambdaValue zipSelector]:
                if (args[0] is IEnumerable otherEnumerable && args[0] is not string)
                {
                    var otherList = otherEnumerable.Cast<object?>().ToList();
                    return (true,
                        list.Zip(otherList, (first, second) => InvokeLambda(zipSelector, [first, second])).ToList());
                }

                throw new EvalException(
                    $"Zip requires an enumerable as first argument, got {args[0]?.GetType().Name ?? "null"}");

            case "zip" when args.Length == 1:
                if (args[0] is IEnumerable zipEnumerable && args[0] is not string)
                {
                    var otherList = zipEnumerable.Cast<object?>().ToList();
                    // Return tuples as dictionaries, cast to List<object?> for consistency
                    return (true, list.Zip(otherList, (first, second) => (object?)new Dictionary<string, object?>
                    {
                        ["First"] = first,
                        ["Second"] = second
                    }).ToList());
                }

                throw new EvalException(
                    $"Zip requires an enumerable argument, got {args[0]?.GetType().Name ?? "null"}");
        }

        return (false, null);
    }
}