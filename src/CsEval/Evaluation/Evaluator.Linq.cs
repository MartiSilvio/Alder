namespace CsEval.Evaluation;

public sealed partial class Evaluator
{
    private static bool IsEnumerableMethod(string name) => name.ToLowerInvariant() switch
    {
        "where" or "select" or "aggregate" or
            "first" or "firstordefault" or "last" or "lastordefault" or
            "single" or "singleordefault" or
            "any" or "all" or "count" or "sum" or "average" or
            "min" or "max" or "orderby" or "orderbydescending" or
            "thenby" or "thenbydescending" or
            "distinct" or "take" or "skip" or "contains" or "reverse" or
            "tolist" or "toarray" or "cast" or "concat" or
            "groupby" or "selectmany" or "zip" => true,
        _ => false
    };

    private (bool Success, object? Value) TryInvokeEnumerableMethod(IEnumerable enumerable, string methodName, object?[] args)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        var list = enumerable.Cast<object?>().ToList();

        switch (methodName.ToLowerInvariant())
        {
            case "where" when args.Length == 1 && args[0] is LambdaValue predicate:
                return (true, list.Where(item => IsTruthy(InvokeLambda(predicate, [item]))).ToList());

            case "select" when args.Length == 1 && args[0] is LambdaValue selector:
                return (true, list.Select(item => InvokeLambda(selector, [item])).ToList());

            case "aggregate" when args.Length == 2 && args[0] is LambdaValue aggregator:
                return (true, list.Aggregate(args[1], (acc, item) => InvokeLambda(aggregator, [acc, item])));

            case "aggregate" when args.Length == 1 && args[0] is LambdaValue reducer:
                return (true, list.Skip(1).Aggregate(list.FirstOrDefault(), (acc, item) => InvokeLambda(reducer, [acc, item])));

            case "first":
                if (args.Length == 1 && args[0] is LambdaValue firstPredicate)
                    return (true, list.First(item => IsTruthy(InvokeLambda(firstPredicate, [item]))));
                return (true, list.First());

            case "firstordefault":
                if (args.Length == 1 && args[0] is LambdaValue firstOrDefaultPredicate)
                    return (true, list.FirstOrDefault(item => IsTruthy(InvokeLambda(firstOrDefaultPredicate, [item]))));
                return (true, list.FirstOrDefault());

            case "last":
                if (args.Length == 1 && args[0] is LambdaValue lastPredicate)
                    return (true, list.Last(item => IsTruthy(InvokeLambda(lastPredicate, [item]))));
                return (true, list.Last());

            case "lastordefault":
                if (args.Length == 1 && args[0] is LambdaValue lastOrDefaultPredicate)
                    return (true, list.LastOrDefault(item => IsTruthy(InvokeLambda(lastOrDefaultPredicate, [item]))));
                return (true, list.LastOrDefault());

            case "single":
                if (args.Length == 1 && args[0] is LambdaValue singlePredicate)
                    return (true, list.Single(item => IsTruthy(InvokeLambda(singlePredicate, [item]))));
                return (true, list.Single());

            case "singleordefault":
                if (args.Length == 1 && args[0] is LambdaValue singleOrDefaultPredicate)
                    return (true, list.SingleOrDefault(item => IsTruthy(InvokeLambda(singleOrDefaultPredicate, [item]))));
                return (true, list.SingleOrDefault());

            case "any":
                if (args.Length == 1 && args[0] is LambdaValue anyPredicate)
                    return (true, list.Any(item => IsTruthy(InvokeLambda(anyPredicate, [item]))));
                return (true, list.Any());

            case "all" when args.Length == 1 && args[0] is LambdaValue allPredicate:
                return (true, list.All(item => IsTruthy(InvokeLambda(allPredicate, [item]))));

            case "count":
                if (args.Length == 1 && args[0] is LambdaValue countPredicate)
                    return (true, list.Count(item => IsTruthy(InvokeLambda(countPredicate, [item]))));
                return (true, list.Count);

            case "sum":
                if (args.Length == 1 && args[0] is LambdaValue sumSelector)
                    return (true, list.Sum(item => ToDouble(InvokeLambda(sumSelector, [item]))));
                return (true, list.Sum(item => ToDouble(item)));

            case "average":
                if (args.Length == 1 && args[0] is LambdaValue avgSelector)
                    return (true, list.Average(item => ToDouble(InvokeLambda(avgSelector, [item]))));
                return (true, list.Average(item => ToDouble(item)));

            case "min":
                if (args.Length == 1 && args[0] is LambdaValue minSelector)
                    return (true, list.Min(item => InvokeLambda(minSelector, [item])));
                return (true, list.Min());

            case "max":
                if (args.Length == 1 && args[0] is LambdaValue maxSelector)
                    return (true, list.Max(item => InvokeLambda(maxSelector, [item])));
                return (true, list.Max());

            case "orderby" when args.Length == 1 && args[0] is LambdaValue orderSelector:
                return (true, list.OrderBy(item => InvokeLambda(orderSelector, [item])).ToList());

            case "orderbydescending" when args.Length == 1 && args[0] is LambdaValue orderDescSelector:
                return (true, list.OrderByDescending(item => InvokeLambda(orderDescSelector, [item])).ToList());

            case "distinct":
                return (true, list.Distinct().ToList());

            case "take" when args.Length == 1:
                return (true, list.Take(Convert.ToInt32(args[0])).ToList());

            case "skip" when args.Length == 1:
                return (true, list.Skip(Convert.ToInt32(args[0])).ToList());

            case "contains" when args.Length == 1:
                return (true, list.Contains(args[0]));

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
                throw new EvalException($"Concat requires an enumerable argument, got {args[0]?.GetType().Name ?? "null"}");
        }

        return (false, null);
    }
}
