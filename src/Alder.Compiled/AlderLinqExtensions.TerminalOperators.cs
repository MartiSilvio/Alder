using System.Linq.Expressions;

namespace Alder.Compiled;

public static partial class AlderLinqExtensions
{
    public static bool AnyDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.Any(CompilePredicate<T>(null, predicate, variables));

    public static bool AnyDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Any(CompilePredicate<T>(engine, predicate, variables));

    public static bool AnyDynamic<T>(this IEnumerable<T> source, Expression<Func<T, bool>> predicateExpr)
        => source.Any(CompilePredicate(predicateExpr));

    public static bool AnyDynamic<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return source.Any(predicate);
    }

    public static bool AnyDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.Any(ParsePredicate<T>(null, predicate, variables));

    public static bool AnyDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Any(ParsePredicate<T>(engine, predicate, variables));

    public static bool AnyDynamic<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicateExpr)
    {
        ArgumentNullException.ThrowIfNull(predicateExpr);
        return source.Any(predicateExpr);
    }

    public static ValueTask<bool> AnyDynamic<T>(
        this IAsyncEnumerable<T> source,
        string predicate,
        params object?[] variables)
        => source.AnyDynamic(GetGlobalEngine(), predicate, variables);

    public static async ValueTask<bool> AnyDynamic<T>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string predicate,
        params object?[] variables)
    {
        var compiled = CompilePredicate<T>(engine, predicate, variables);
        await foreach (var item in source)
            if (compiled(item))
                return true;
        return false;
    }

    public static ValueTask<bool> AnyDynamic<T>(
        this IAsyncEnumerable<T> source,
        Expression<Func<T, bool>> predicateExpr)
        => source.AnyDynamic(CompilePredicate(predicateExpr));

    public static async ValueTask<bool> AnyDynamic<T>(
        this IAsyncEnumerable<T> source,
        Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        await foreach (var item in source)
            if (predicate(item))
                return true;
        return false;
    }

    public static bool AllDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.All(CompilePredicate<T>(null, predicate, variables));

    public static bool AllDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.All(CompilePredicate<T>(engine, predicate, variables));

    public static bool AllDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.All(ParsePredicate<T>(null, predicate, variables));

    public static bool AllDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.All(ParsePredicate<T>(engine, predicate, variables));

    public static ValueTask<bool> AllDynamic<T>(
        this IAsyncEnumerable<T> source,
        string predicate,
        params object?[] variables)
        => source.AllDynamic(GetGlobalEngine(), predicate, variables);

    public static async ValueTask<bool> AllDynamic<T>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string predicate,
        params object?[] variables)
    {
        var compiled = CompilePredicate<T>(engine, predicate, variables);
        await foreach (var item in source)
            if (!compiled(item))
                return false;
        return true;
    }

    public static T FirstDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.First(CompilePredicate<T>(null, predicate, variables));

    public static T FirstDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.First(CompilePredicate<T>(engine, predicate, variables));

    public static T? FirstOrDefaultDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.FirstOrDefault(CompilePredicate<T>(null, predicate, variables));

    public static T? FirstOrDefaultDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.FirstOrDefault(CompilePredicate<T>(engine, predicate, variables));

    public static T LastDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.Last(CompilePredicate<T>(null, predicate, variables));

    public static T LastDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Last(CompilePredicate<T>(engine, predicate, variables));

    public static T? LastOrDefaultDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.LastOrDefault(CompilePredicate<T>(null, predicate, variables));

    public static T? LastOrDefaultDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.LastOrDefault(CompilePredicate<T>(engine, predicate, variables));

    public static T SingleDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.Single(CompilePredicate<T>(null, predicate, variables));

    public static T SingleDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Single(CompilePredicate<T>(engine, predicate, variables));

    public static T? SingleOrDefaultDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.SingleOrDefault(CompilePredicate<T>(null, predicate, variables));

    public static T? SingleOrDefaultDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.SingleOrDefault(CompilePredicate<T>(engine, predicate, variables));

    public static T FirstDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.First(ParsePredicate<T>(null, predicate, variables));

    public static T FirstDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.First(ParsePredicate<T>(engine, predicate, variables));

    public static T? FirstOrDefaultDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.FirstOrDefault(ParsePredicate<T>(null, predicate, variables));

    public static T? FirstOrDefaultDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.FirstOrDefault(ParsePredicate<T>(engine, predicate, variables));

    public static T LastDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.Last(ParsePredicate<T>(null, predicate, variables));

    public static T LastDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Last(ParsePredicate<T>(engine, predicate, variables));

    public static T? LastOrDefaultDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.LastOrDefault(ParsePredicate<T>(null, predicate, variables));

    public static T? LastOrDefaultDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.LastOrDefault(ParsePredicate<T>(engine, predicate, variables));

    public static T SingleDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.Single(ParsePredicate<T>(null, predicate, variables));

    public static T SingleDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Single(ParsePredicate<T>(engine, predicate, variables));

    public static T? SingleOrDefaultDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.SingleOrDefault(ParsePredicate<T>(null, predicate, variables));

    public static T? SingleOrDefaultDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.SingleOrDefault(ParsePredicate<T>(engine, predicate, variables));

    public static ValueTask<T> FirstDynamic<T>(
        this IAsyncEnumerable<T> source,
        string predicate,
        params object?[] variables)
        => source.FirstDynamic(GetGlobalEngine(), predicate, variables);

    public static async ValueTask<T> FirstDynamic<T>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string predicate,
        params object?[] variables)
    {
        var compiled = CompilePredicate<T>(engine, predicate, variables);
        await foreach (var item in source)
            if (compiled(item))
                return item;
        throw new InvalidOperationException("Sequence contains no matching element");
    }

    public static ValueTask<T?> FirstOrDefaultDynamic<T>(
        this IAsyncEnumerable<T> source,
        string predicate,
        params object?[] variables)
        => source.FirstOrDefaultDynamic(GetGlobalEngine(), predicate, variables);

    public static async ValueTask<T?> FirstOrDefaultDynamic<T>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string predicate,
        params object?[] variables)
    {
        var compiled = CompilePredicate<T>(engine, predicate, variables);
        await foreach (var item in source)
            if (compiled(item))
                return item;
        return default;
    }

    public static ValueTask<T> LastDynamic<T>(
        this IAsyncEnumerable<T> source,
        string predicate,
        params object?[] variables)
        => source.LastDynamic(GetGlobalEngine(), predicate, variables);

    public static async ValueTask<T> LastDynamic<T>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string predicate,
        params object?[] variables)
    {
        var compiled = CompilePredicate<T>(engine, predicate, variables);
        var found = false;
        var last = default(T)!;
        await foreach (var item in source)
            if (compiled(item))
            {
                last = item;
                found = true;
            }

        if (!found)
            throw new InvalidOperationException("Sequence contains no matching element");

        return last;
    }

    public static ValueTask<T> SingleDynamic<T>(
        this IAsyncEnumerable<T> source,
        string predicate,
        params object?[] variables)
        => source.SingleDynamic(GetGlobalEngine(), predicate, variables);

    public static async ValueTask<T> SingleDynamic<T>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string predicate,
        params object?[] variables)
    {
        var compiled = CompilePredicate<T>(engine, predicate, variables);
        var found = false;
        var single = default(T)!;
        await foreach (var item in source)
        {
            if (!compiled(item))
                continue;

            if (found)
                throw new InvalidOperationException("Sequence contains more than one matching element");

            single = item;
            found = true;
        }

        if (!found)
            throw new InvalidOperationException("Sequence contains no matching element");

        return single;
    }

    public static int CountDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.Count(CompilePredicate<T>(null, predicate, variables));

    public static int CountDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Count(CompilePredicate<T>(engine, predicate, variables));

    public static int CountDynamic<T>(this IEnumerable<T> source, Expression<Func<T, bool>> predicateExpr)
        => source.Count(CompilePredicate(predicateExpr));

    public static int CountDynamic<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return source.Count(predicate);
    }

    public static int CountDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.Count(ParsePredicate<T>(null, predicate, variables));

    public static int CountDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Count(ParsePredicate<T>(engine, predicate, variables));

    public static int CountDynamic<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicateExpr)
    {
        ArgumentNullException.ThrowIfNull(predicateExpr);
        return source.Count(predicateExpr);
    }

    public static ValueTask<int> CountDynamic<T>(
        this IAsyncEnumerable<T> source,
        string predicate,
        params object?[] variables)
        => source.CountDynamic(GetGlobalEngine(), predicate, variables);

    public static async ValueTask<int> CountDynamic<T>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string predicate,
        params object?[] variables)
    {
        var compiled = CompilePredicate<T>(engine, predicate, variables);
        var count = 0;
        await foreach (var item in source)
            if (compiled(item))
                count++;
        return count;
    }

    public static ValueTask<int> CountDynamic<T>(
        this IAsyncEnumerable<T> source,
        Expression<Func<T, bool>> predicateExpr)
        => source.CountDynamic(CompilePredicate(predicateExpr));

    public static async ValueTask<int> CountDynamic<T>(
        this IAsyncEnumerable<T> source,
        Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var count = 0;
        await foreach (var item in source)
            if (predicate(item))
                count++;
        return count;
    }

    public static long LongCountDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.LongCount(CompilePredicate<T>(null, predicate, variables));

    public static long LongCountDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.LongCount(CompilePredicate<T>(engine, predicate, variables));

    public static long LongCountDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.LongCount(ParsePredicate<T>(null, predicate, variables));

    public static long LongCountDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.LongCount(ParsePredicate<T>(engine, predicate, variables));

    public static ValueTask<long> LongCountDynamic<T>(
        this IAsyncEnumerable<T> source,
        string predicate,
        params object?[] variables)
        => source.LongCountDynamic(GetGlobalEngine(), predicate, variables);

    public static async ValueTask<long> LongCountDynamic<T>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string predicate,
        params object?[] variables)
    {
        var compiled = CompilePredicate<T>(engine, predicate, variables);
        var count = 0L;
        await foreach (var item in source)
            if (compiled(item))
                count++;
        return count;
    }

    public static decimal SumDynamic<T>(this IEnumerable<T> source, string selector, params object?[] variables)
        => source.Sum(CompileSelector<T, decimal>(null, selector, variables));

    public static decimal SumDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Sum(CompileSelector<T, decimal>(engine, selector, variables));

    public static decimal SumDynamic<T>(this IEnumerable<T> source, Expression<Func<T, decimal>> selectorExpr)
        => source.Sum(CompileSelector(selectorExpr));

    public static decimal SumDynamic<T>(this IEnumerable<T> source, Func<T, decimal> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return source.Sum(selector);
    }

    public static decimal SumDynamic<T>(this IQueryable<T> source, string selector, params object?[] variables)
        => source.Sum(ParseSelector<T, decimal>(null, selector, variables));

    public static decimal SumDynamic<T>(this IQueryable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Sum(ParseSelector<T, decimal>(engine, selector, variables));

    public static decimal SumDynamic<T>(this IQueryable<T> source, Expression<Func<T, decimal>> selectorExpr)
    {
        ArgumentNullException.ThrowIfNull(selectorExpr);
        return source.Sum(selectorExpr);
    }

    public static ValueTask<decimal> SumDynamic<T>(
        this IAsyncEnumerable<T> source,
        string selector,
        params object?[] variables)
        => source.SumDynamic(GetGlobalEngine(), selector, variables);

    public static async ValueTask<decimal> SumDynamic<T>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string selector,
        params object?[] variables)
    {
        var compiled = CompileSelector<T, decimal>(engine, selector, variables);
        var sum = 0m;
        await foreach (var item in source)
            sum += compiled(item);
        return sum;
    }

    public static ValueTask<decimal> SumDynamic<T>(
        this IAsyncEnumerable<T> source,
        Expression<Func<T, decimal>> selectorExpr)
        => source.SumDynamic(CompileSelector(selectorExpr));

    public static async ValueTask<decimal> SumDynamic<T>(
        this IAsyncEnumerable<T> source,
        Func<T, decimal> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var sum = 0m;
        await foreach (var item in source)
            sum += selector(item);
        return sum;
    }

    public static double AverageDynamic<T>(this IEnumerable<T> source, string selector, params object?[] variables)
        => source.Average(CompileSelector<T, double>(null, selector, variables));

    public static double AverageDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Average(CompileSelector<T, double>(engine, selector, variables));

    public static double AverageDynamic<T>(this IQueryable<T> source, string selector, params object?[] variables)
        => source.Average(ParseSelector<T, double>(null, selector, variables));

    public static double AverageDynamic<T>(this IQueryable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Average(ParseSelector<T, double>(engine, selector, variables));

    public static ValueTask<double> AverageDynamic<T>(
        this IAsyncEnumerable<T> source,
        string selector,
        params object?[] variables)
        => source.AverageDynamic(GetGlobalEngine(), selector, variables);

    public static async ValueTask<double> AverageDynamic<T>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string selector,
        params object?[] variables)
    {
        var compiled = CompileSelector<T, double>(engine, selector, variables);
        var sum = 0.0;
        var count = 0L;
        await foreach (var item in source)
        {
            sum += compiled(item);
            count++;
        }

        if (count == 0)
            throw new InvalidOperationException("Sequence contains no elements");

        return sum / count;
    }

    public static TResult MinDynamic<T, TResult>(this IEnumerable<T> source, string selector, params object?[] variables)
        => source.Min(CompileSelector<T, TResult>(null, selector, variables))!;

    public static TResult MinDynamic<T, TResult>(this IEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Min(CompileSelector<T, TResult>(engine, selector, variables))!;

    public static TResult MinDynamic<T, TResult>(this IQueryable<T> source, string selector, params object?[] variables)
        => source.Min(ParseSelector<T, TResult>(null, selector, variables))!;

    public static TResult MinDynamic<T, TResult>(this IQueryable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Min(ParseSelector<T, TResult>(engine, selector, variables))!;

    public static ValueTask<TResult> MinDynamic<T, TResult>(
        this IAsyncEnumerable<T> source,
        string selector,
        params object?[] variables)
        => source.MinDynamic<T, TResult>(GetGlobalEngine(), selector, variables);

    public static async ValueTask<TResult> MinDynamic<T, TResult>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string selector,
        params object?[] variables)
    {
        var compiled = CompileSelector<T, TResult>(engine, selector, variables);
        var comparer = Comparer<TResult>.Default;
        var hasValue = false;
        TResult? min = default;

        await foreach (var item in source)
        {
            var value = compiled(item);
            if (!hasValue || comparer.Compare(value, min!) < 0)
            {
                min = value;
                hasValue = true;
            }
        }

        if (!hasValue)
            throw new InvalidOperationException("Sequence contains no elements");

        return min!;
    }

    public static TResult MaxDynamic<T, TResult>(this IEnumerable<T> source, string selector, params object?[] variables)
        => source.Max(CompileSelector<T, TResult>(null, selector, variables))!;

    public static TResult MaxDynamic<T, TResult>(this IEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Max(CompileSelector<T, TResult>(engine, selector, variables))!;

    public static TResult MaxDynamic<T, TResult>(this IQueryable<T> source, string selector, params object?[] variables)
        => source.Max(ParseSelector<T, TResult>(null, selector, variables))!;

    public static TResult MaxDynamic<T, TResult>(this IQueryable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Max(ParseSelector<T, TResult>(engine, selector, variables))!;

    public static ValueTask<TResult> MaxDynamic<T, TResult>(
        this IAsyncEnumerable<T> source,
        string selector,
        params object?[] variables)
        => source.MaxDynamic<T, TResult>(GetGlobalEngine(), selector, variables);

    public static async ValueTask<TResult> MaxDynamic<T, TResult>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string selector,
        params object?[] variables)
    {
        var compiled = CompileSelector<T, TResult>(engine, selector, variables);
        var comparer = Comparer<TResult>.Default;
        var hasValue = false;
        TResult? max = default;

        await foreach (var item in source)
        {
            var value = compiled(item);
            if (!hasValue || comparer.Compare(value, max!) > 0)
            {
                max = value;
                hasValue = true;
            }
        }

        if (!hasValue)
            throw new InvalidOperationException("Sequence contains no elements");

        return max!;
    }
}
