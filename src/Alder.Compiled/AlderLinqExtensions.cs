using System.Linq.Expressions;

namespace Alder.Compiled;

public static class AlderLinqExtensions
{
    private static AlderEngine GetGlobalEngine()
    {
        var engine = AlderEval.GetEngine();
        if (!engine.HasCompiler)
            throw new InvalidOperationException(
                "LINQ Dynamic methods require a compiler. " +
                "Call AlderEval.Configure(o => o.UseCompiler()) before using WhereDynamic, SelectDynamic, etc.");
        return engine;
    }

    private static AlderEngine ValidateEngine(AlderEngine engine)
    {
        if (!engine.HasCompiler)
            throw new InvalidOperationException(
                "LINQ Dynamic methods require a compiler. " +
                "Call engine options UseCompiler() before using WhereDynamic, SelectDynamic, etc.");
        return engine;
    }

    private static AlderEngine ResolveEngine(AlderEngine? engine) =>
        engine != null ? ValidateEngine(engine) : GetGlobalEngine();

    private static Dictionary<string, object?>? BuildPositionalVars(object?[] variables) =>
        variables.Length == 0 ? null : AlderEngine.BuildPositionalVariables(variables);

    private static Func<T, bool> CompilePredicate<T>(AlderEngine engine, string predicate, Dictionary<string, object?>? vars)
        => AlderCompiledEngineExtensions.CompileExpression<Func<T, bool>>(engine, predicate, vars);

    private static Func<T, TResult> CompileSelector<T, TResult>(AlderEngine engine, string selector, Dictionary<string, object?>? vars)
        => AlderCompiledEngineExtensions.CompileExpression<Func<T, TResult>>(engine, selector, vars);

    private static Expression<Func<T, bool>> ParsePredicate<T>(AlderEngine engine, string predicate, Dictionary<string, object?>? vars)
        => AlderCompiledEngineExtensions.ParseAsExpression<Func<T, bool>>(engine, predicate, vars);

    private static Expression<Func<T, TResult>> ParseSelector<T, TResult>(AlderEngine engine, string selector, Dictionary<string, object?>? vars)
        => AlderCompiledEngineExtensions.ParseAsExpression<Func<T, TResult>>(engine, selector, vars);

    #region IEnumerable<T> — Filtering

    public static IEnumerable<T> WhereDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.Where(CompilePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static IEnumerable<T> WhereDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Where(CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    #endregion

    #region IEnumerable<T> — Projection

    public static IEnumerable<TResult> SelectDynamic<T, TResult>(this IEnumerable<T> source, string selector, params object?[] variables)
        => source.Select(CompileSelector<T, TResult>(GetGlobalEngine(), selector, BuildPositionalVars(variables)));

    public static IEnumerable<TResult> SelectDynamic<T, TResult>(this IEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Select(CompileSelector<T, TResult>(ValidateEngine(engine), selector, BuildPositionalVars(variables)));

    #endregion

    #region IEnumerable<T> — Ordering

    public static IOrderedEnumerable<T> OrderByDynamic<T, TKey>(this IEnumerable<T> source, string keySelector, params object?[] variables)
        => source.OrderBy(CompileSelector<T, TKey>(GetGlobalEngine(), keySelector, BuildPositionalVars(variables)));

    public static IOrderedEnumerable<T> OrderByDynamic<T, TKey>(this IEnumerable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.OrderBy(CompileSelector<T, TKey>(ValidateEngine(engine), keySelector, BuildPositionalVars(variables)));

    public static IOrderedEnumerable<T> OrderByDescendingDynamic<T, TKey>(this IEnumerable<T> source, string keySelector, params object?[] variables)
        => source.OrderByDescending(CompileSelector<T, TKey>(GetGlobalEngine(), keySelector, BuildPositionalVars(variables)));

    public static IOrderedEnumerable<T> OrderByDescendingDynamic<T, TKey>(this IEnumerable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.OrderByDescending(CompileSelector<T, TKey>(ValidateEngine(engine), keySelector, BuildPositionalVars(variables)));

    public static IOrderedEnumerable<T> ThenByDynamic<T, TKey>(this IOrderedEnumerable<T> source, string keySelector, params object?[] variables)
        => source.ThenBy(CompileSelector<T, TKey>(GetGlobalEngine(), keySelector, BuildPositionalVars(variables)));

    public static IOrderedEnumerable<T> ThenByDynamic<T, TKey>(this IOrderedEnumerable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.ThenBy(CompileSelector<T, TKey>(ValidateEngine(engine), keySelector, BuildPositionalVars(variables)));

    public static IOrderedEnumerable<T> ThenByDescendingDynamic<T, TKey>(this IOrderedEnumerable<T> source, string keySelector, params object?[] variables)
        => source.ThenByDescending(CompileSelector<T, TKey>(GetGlobalEngine(), keySelector, BuildPositionalVars(variables)));

    public static IOrderedEnumerable<T> ThenByDescendingDynamic<T, TKey>(this IOrderedEnumerable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.ThenByDescending(CompileSelector<T, TKey>(ValidateEngine(engine), keySelector, BuildPositionalVars(variables)));

    #endregion

    #region IEnumerable<T> — Quantifiers

    public static bool AnyDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.Any(CompilePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static bool AnyDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Any(CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    public static bool AllDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.All(CompilePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static bool AllDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.All(CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    public static int CountDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.Count(CompilePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static int CountDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Count(CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    #endregion

    #region IEnumerable<T> — Element access

    public static T FirstDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.First(CompilePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static T FirstDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.First(CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    public static T? FirstOrDefaultDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.FirstOrDefault(CompilePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static T? FirstOrDefaultDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.FirstOrDefault(CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    public static T LastDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.Last(CompilePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static T LastDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Last(CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    public static T? LastOrDefaultDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.LastOrDefault(CompilePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static T? LastOrDefaultDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.LastOrDefault(CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    public static T SingleDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.Single(CompilePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static T SingleDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Single(CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    public static T? SingleOrDefaultDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.SingleOrDefault(CompilePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static T? SingleOrDefaultDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.SingleOrDefault(CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    #endregion

    #region IEnumerable<T> — Grouping & Distinct

    public static IEnumerable<IGrouping<TKey, T>> GroupByDynamic<T, TKey>(this IEnumerable<T> source, string keySelector, params object?[] variables)
        => source.GroupBy(CompileSelector<T, TKey>(GetGlobalEngine(), keySelector, BuildPositionalVars(variables)));

    public static IEnumerable<IGrouping<TKey, T>> GroupByDynamic<T, TKey>(this IEnumerable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.GroupBy(CompileSelector<T, TKey>(ValidateEngine(engine), keySelector, BuildPositionalVars(variables)));

    public static IEnumerable<T> DistinctByDynamic<T, TKey>(this IEnumerable<T> source, string keySelector, params object?[] variables)
        => source.DistinctBy(CompileSelector<T, TKey>(GetGlobalEngine(), keySelector, BuildPositionalVars(variables)));

    public static IEnumerable<T> DistinctByDynamic<T, TKey>(this IEnumerable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.DistinctBy(CompileSelector<T, TKey>(ValidateEngine(engine), keySelector, BuildPositionalVars(variables)));

    #endregion

    #region IEnumerable<T> — Aggregation

    public static decimal SumDynamic<T>(this IEnumerable<T> source, string selector, params object?[] variables)
        => source.Sum(CompileSelector<T, decimal>(GetGlobalEngine(), selector, BuildPositionalVars(variables)));

    public static decimal SumDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Sum(CompileSelector<T, decimal>(ValidateEngine(engine), selector, BuildPositionalVars(variables)));

    public static double AverageDynamic<T>(this IEnumerable<T> source, string selector, params object?[] variables)
        => source.Average(CompileSelector<T, double>(GetGlobalEngine(), selector, BuildPositionalVars(variables)));

    public static double AverageDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Average(CompileSelector<T, double>(ValidateEngine(engine), selector, BuildPositionalVars(variables)));

    public static TResult MinDynamic<T, TResult>(this IEnumerable<T> source, string selector, params object?[] variables)
        => source.Min(CompileSelector<T, TResult>(GetGlobalEngine(), selector, BuildPositionalVars(variables)))!;

    public static TResult MinDynamic<T, TResult>(this IEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Min(CompileSelector<T, TResult>(ValidateEngine(engine), selector, BuildPositionalVars(variables)))!;

    public static TResult MaxDynamic<T, TResult>(this IEnumerable<T> source, string selector, params object?[] variables)
        => source.Max(CompileSelector<T, TResult>(GetGlobalEngine(), selector, BuildPositionalVars(variables)))!;

    public static TResult MaxDynamic<T, TResult>(this IEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Max(CompileSelector<T, TResult>(ValidateEngine(engine), selector, BuildPositionalVars(variables)))!;

    #endregion

    #region IQueryable<T> — Filtering

    public static IQueryable<T> WhereDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.Where(ParsePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static IQueryable<T> WhereDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Where(ParsePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    #endregion

    #region IQueryable<T> — Projection

    public static IQueryable<TResult> SelectDynamic<T, TResult>(this IQueryable<T> source, string selector, params object?[] variables)
        => source.Select(ParseSelector<T, TResult>(GetGlobalEngine(), selector, BuildPositionalVars(variables)));

    public static IQueryable<TResult> SelectDynamic<T, TResult>(this IQueryable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Select(ParseSelector<T, TResult>(ValidateEngine(engine), selector, BuildPositionalVars(variables)));

    #endregion

    #region IQueryable<T> — Ordering

    public static IOrderedQueryable<T> OrderByDynamic<T, TKey>(this IQueryable<T> source, string keySelector, params object?[] variables)
        => source.OrderBy(ParseSelector<T, TKey>(GetGlobalEngine(), keySelector, BuildPositionalVars(variables)));

    public static IOrderedQueryable<T> OrderByDynamic<T, TKey>(this IQueryable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.OrderBy(ParseSelector<T, TKey>(ValidateEngine(engine), keySelector, BuildPositionalVars(variables)));

    public static IOrderedQueryable<T> OrderByDescendingDynamic<T, TKey>(this IQueryable<T> source, string keySelector, params object?[] variables)
        => source.OrderByDescending(ParseSelector<T, TKey>(GetGlobalEngine(), keySelector, BuildPositionalVars(variables)));

    public static IOrderedQueryable<T> OrderByDescendingDynamic<T, TKey>(this IQueryable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.OrderByDescending(ParseSelector<T, TKey>(ValidateEngine(engine), keySelector, BuildPositionalVars(variables)));

    public static IOrderedQueryable<T> ThenByDynamic<T, TKey>(this IOrderedQueryable<T> source, string keySelector, params object?[] variables)
        => source.ThenBy(ParseSelector<T, TKey>(GetGlobalEngine(), keySelector, BuildPositionalVars(variables)));

    public static IOrderedQueryable<T> ThenByDynamic<T, TKey>(this IOrderedQueryable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.ThenBy(ParseSelector<T, TKey>(ValidateEngine(engine), keySelector, BuildPositionalVars(variables)));

    public static IOrderedQueryable<T> ThenByDescendingDynamic<T, TKey>(this IOrderedQueryable<T> source, string keySelector, params object?[] variables)
        => source.ThenByDescending(ParseSelector<T, TKey>(GetGlobalEngine(), keySelector, BuildPositionalVars(variables)));

    public static IOrderedQueryable<T> ThenByDescendingDynamic<T, TKey>(this IOrderedQueryable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.ThenByDescending(ParseSelector<T, TKey>(ValidateEngine(engine), keySelector, BuildPositionalVars(variables)));

    #endregion

    #region IQueryable<T> — Quantifiers & Count

    public static bool AnyDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.Any(ParsePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static bool AnyDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Any(ParsePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    public static bool AllDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.All(ParsePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static bool AllDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.All(ParsePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    public static int CountDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.Count(ParsePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static int CountDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Count(ParsePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    #endregion

    #region IQueryable<T> — Element access

    public static T FirstDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.First(ParsePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static T FirstDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.First(ParsePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    public static T? FirstOrDefaultDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.FirstOrDefault(ParsePredicate<T>(GetGlobalEngine(), predicate, BuildPositionalVars(variables)));

    public static T? FirstOrDefaultDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.FirstOrDefault(ParsePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables)));

    #endregion

    #region IQueryable<T> — Grouping

    public static IQueryable<IGrouping<TKey, T>> GroupByDynamic<T, TKey>(this IQueryable<T> source, string keySelector, params object?[] variables)
        => source.GroupBy(ParseSelector<T, TKey>(GetGlobalEngine(), keySelector, BuildPositionalVars(variables)));

    public static IQueryable<IGrouping<TKey, T>> GroupByDynamic<T, TKey>(this IQueryable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.GroupBy(ParseSelector<T, TKey>(ValidateEngine(engine), keySelector, BuildPositionalVars(variables)));

    #endregion

    #region IQueryable<T> — Aggregation

    public static decimal SumDynamic<T>(this IQueryable<T> source, string selector, params object?[] variables)
        => source.Sum(ParseSelector<T, decimal>(GetGlobalEngine(), selector, BuildPositionalVars(variables)));

    public static decimal SumDynamic<T>(this IQueryable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Sum(ParseSelector<T, decimal>(ValidateEngine(engine), selector, BuildPositionalVars(variables)));

    public static double AverageDynamic<T>(this IQueryable<T> source, string selector, params object?[] variables)
        => source.Average(ParseSelector<T, double>(GetGlobalEngine(), selector, BuildPositionalVars(variables)));

    public static double AverageDynamic<T>(this IQueryable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Average(ParseSelector<T, double>(ValidateEngine(engine), selector, BuildPositionalVars(variables)));

    public static TResult MinDynamic<T, TResult>(this IQueryable<T> source, string selector, params object?[] variables)
        => source.Min(ParseSelector<T, TResult>(GetGlobalEngine(), selector, BuildPositionalVars(variables)))!;

    public static TResult MinDynamic<T, TResult>(this IQueryable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Min(ParseSelector<T, TResult>(ValidateEngine(engine), selector, BuildPositionalVars(variables)))!;

    public static TResult MaxDynamic<T, TResult>(this IQueryable<T> source, string selector, params object?[] variables)
        => source.Max(ParseSelector<T, TResult>(GetGlobalEngine(), selector, BuildPositionalVars(variables)))!;

    public static TResult MaxDynamic<T, TResult>(this IQueryable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Max(ParseSelector<T, TResult>(ValidateEngine(engine), selector, BuildPositionalVars(variables)))!;

    #endregion

    #region IAsyncEnumerable<T> — Filtering

    public static IAsyncEnumerable<T> WhereDynamic<T>(
        this IAsyncEnumerable<T> source, string predicate, params object?[] variables)
        => source.WhereDynamic(GetGlobalEngine(), predicate, variables);

    public static async IAsyncEnumerable<T> WhereDynamic<T>(
        this IAsyncEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
    {
        var compiled = CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables));
        await foreach (var item in source)
            if (compiled(item))
                yield return item;
    }

    #endregion

    #region IAsyncEnumerable<T> — Projection

    public static IAsyncEnumerable<TResult> SelectDynamic<T, TResult>(
        this IAsyncEnumerable<T> source, string selector, params object?[] variables)
        => source.SelectDynamic<T, TResult>(GetGlobalEngine(), selector, variables);

    public static async IAsyncEnumerable<TResult> SelectDynamic<T, TResult>(
        this IAsyncEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
    {
        var compiled = CompileSelector<T, TResult>(ValidateEngine(engine), selector, BuildPositionalVars(variables));
        await foreach (var item in source)
            yield return compiled(item);
    }

    #endregion

    #region IAsyncEnumerable<T> — Quantifiers & Count

    public static ValueTask<bool> AnyDynamic<T>(
        this IAsyncEnumerable<T> source, string predicate, params object?[] variables)
        => source.AnyDynamic(GetGlobalEngine(), predicate, variables);

    public static async ValueTask<bool> AnyDynamic<T>(
        this IAsyncEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
    {
        var compiled = CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables));
        await foreach (var item in source)
            if (compiled(item))
                return true;
        return false;
    }

    public static ValueTask<bool> AllDynamic<T>(
        this IAsyncEnumerable<T> source, string predicate, params object?[] variables)
        => source.AllDynamic(GetGlobalEngine(), predicate, variables);

    public static async ValueTask<bool> AllDynamic<T>(
        this IAsyncEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
    {
        var compiled = CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables));
        await foreach (var item in source)
            if (!compiled(item))
                return false;
        return true;
    }

    public static ValueTask<int> CountDynamic<T>(
        this IAsyncEnumerable<T> source, string predicate, params object?[] variables)
        => source.CountDynamic(GetGlobalEngine(), predicate, variables);

    public static async ValueTask<int> CountDynamic<T>(
        this IAsyncEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
    {
        var compiled = CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables));
        var count = 0;
        await foreach (var item in source)
            if (compiled(item))
                count++;
        return count;
    }

    #endregion

    #region IAsyncEnumerable<T> — Element access

    public static ValueTask<T> FirstDynamic<T>(
        this IAsyncEnumerable<T> source, string predicate, params object?[] variables)
        => source.FirstDynamic(GetGlobalEngine(), predicate, variables);

    public static async ValueTask<T> FirstDynamic<T>(
        this IAsyncEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
    {
        var compiled = CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables));
        await foreach (var item in source)
            if (compiled(item))
                return item;
        throw new InvalidOperationException("Sequence contains no matching element");
    }

    public static ValueTask<T?> FirstOrDefaultDynamic<T>(
        this IAsyncEnumerable<T> source, string predicate, params object?[] variables)
        => source.FirstOrDefaultDynamic(GetGlobalEngine(), predicate, variables);

    public static async ValueTask<T?> FirstOrDefaultDynamic<T>(
        this IAsyncEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
    {
        var compiled = CompilePredicate<T>(ValidateEngine(engine), predicate, BuildPositionalVars(variables));
        await foreach (var item in source)
            if (compiled(item))
                return item;
        return default;
    }

    #endregion

    #region IAsyncEnumerable<T> — Aggregation

    public static ValueTask<decimal> SumDynamic<T>(
        this IAsyncEnumerable<T> source, string selector, params object?[] variables)
        => source.SumDynamic(GetGlobalEngine(), selector, variables);

    public static async ValueTask<decimal> SumDynamic<T>(
        this IAsyncEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
    {
        var compiled = CompileSelector<T, decimal>(ValidateEngine(engine), selector, BuildPositionalVars(variables));
        var sum = 0m;
        await foreach (var item in source)
            sum += compiled(item);
        return sum;
    }

    public static ValueTask<double> AverageDynamic<T>(
        this IAsyncEnumerable<T> source, string selector, params object?[] variables)
        => source.AverageDynamic(GetGlobalEngine(), selector, variables);

    public static async ValueTask<double> AverageDynamic<T>(
        this IAsyncEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
    {
        var compiled = CompileSelector<T, double>(ValidateEngine(engine), selector, BuildPositionalVars(variables));
        var sum = 0.0;
        var count = 0L;
        await foreach (var item in source)
        {
            sum += compiled(item);
            count++;
        }
        if (count == 0) throw new InvalidOperationException("Sequence contains no elements");
        return sum / count;
    }

    public static ValueTask<TResult> MinDynamic<T, TResult>(
        this IAsyncEnumerable<T> source, string selector, params object?[] variables)
        => source.MinDynamic<T, TResult>(GetGlobalEngine(), selector, variables);

    public static async ValueTask<TResult> MinDynamic<T, TResult>(
        this IAsyncEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
    {
        var compiled = CompileSelector<T, TResult>(ValidateEngine(engine), selector, BuildPositionalVars(variables));
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
        if (!hasValue) throw new InvalidOperationException("Sequence contains no elements");
        return min!;
    }

    public static ValueTask<TResult> MaxDynamic<T, TResult>(
        this IAsyncEnumerable<T> source, string selector, params object?[] variables)
        => source.MaxDynamic<T, TResult>(GetGlobalEngine(), selector, variables);

    public static async ValueTask<TResult> MaxDynamic<T, TResult>(
        this IAsyncEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
    {
        var compiled = CompileSelector<T, TResult>(ValidateEngine(engine), selector, BuildPositionalVars(variables));
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
        if (!hasValue) throw new InvalidOperationException("Sequence contains no elements");
        return max!;
    }

    #endregion
}
