using System.Linq.Expressions;

namespace Alder.Compiled;

public static class AlderLinqExtensions
{
    private static AlderEngine GetEngine()
    {
        var engine = AlderEval.GetEngine();

        if (!engine.HasCompiler)
            throw new InvalidOperationException(
                "LINQ Dynamic methods require a compiler. " +
                "Call AlderEval.Configure(o => o.UseCompiler()) before using WhereDynamic, SelectDynamic, etc.");

        return engine;
    }

    private static Func<T, bool> CompilePredicate<T>(string predicate)
        => GetEngine().CompileExpression<Func<T, bool>>(predicate);

    private static Func<T, TResult> CompileSelector<T, TResult>(string selector)
        => GetEngine().CompileExpression<Func<T, TResult>>(selector);

    private static Expression<Func<T, bool>> ParsePredicate<T>(string predicate)
        => GetEngine().ParseAsExpression<Func<T, bool>>(predicate);

    private static Expression<Func<T, TResult>> ParseSelector<T, TResult>(string selector)
        => GetEngine().ParseAsExpression<Func<T, TResult>>(selector);

    public static IEnumerable<T> WhereDynamic<T>(this IEnumerable<T> source, string predicate)
        => source.Where(CompilePredicate<T>(predicate));

    public static IEnumerable<TResult> SelectDynamic<T, TResult>(this IEnumerable<T> source, string selector)
        => source.Select(CompileSelector<T, TResult>(selector));

    public static IOrderedEnumerable<T> OrderByDynamic<T, TKey>(this IEnumerable<T> source, string keySelector)
        => source.OrderBy(CompileSelector<T, TKey>(keySelector));

    public static IOrderedEnumerable<T> OrderByDescendingDynamic<T, TKey>(this IEnumerable<T> source, string keySelector)
        => source.OrderByDescending(CompileSelector<T, TKey>(keySelector));

    public static bool AnyDynamic<T>(this IEnumerable<T> source, string predicate)
        => source.Any(CompilePredicate<T>(predicate));

    public static bool AllDynamic<T>(this IEnumerable<T> source, string predicate)
        => source.All(CompilePredicate<T>(predicate));

    public static int CountDynamic<T>(this IEnumerable<T> source, string predicate)
        => source.Count(CompilePredicate<T>(predicate));

    public static T FirstDynamic<T>(this IEnumerable<T> source, string predicate)
        => source.First(CompilePredicate<T>(predicate));

    public static T? FirstOrDefaultDynamic<T>(this IEnumerable<T> source, string predicate)
        => source.FirstOrDefault(CompilePredicate<T>(predicate));

    public static T LastDynamic<T>(this IEnumerable<T> source, string predicate)
        => source.Last(CompilePredicate<T>(predicate));

    public static T? LastOrDefaultDynamic<T>(this IEnumerable<T> source, string predicate)
        => source.LastOrDefault(CompilePredicate<T>(predicate));

    public static T SingleDynamic<T>(this IEnumerable<T> source, string predicate)
        => source.Single(CompilePredicate<T>(predicate));

    public static T? SingleOrDefaultDynamic<T>(this IEnumerable<T> source, string predicate)
        => source.SingleOrDefault(CompilePredicate<T>(predicate));

    public static IQueryable<T> WhereDynamic<T>(this IQueryable<T> source, string predicate)
        => source.Where(ParsePredicate<T>(predicate));

    public static IQueryable<TResult> SelectDynamic<T, TResult>(this IQueryable<T> source, string selector)
        => source.Select(ParseSelector<T, TResult>(selector));

    public static IOrderedQueryable<T> OrderByDynamic<T, TKey>(this IQueryable<T> source, string keySelector)
        => source.OrderBy(ParseSelector<T, TKey>(keySelector));

    public static IOrderedQueryable<T> OrderByDescendingDynamic<T, TKey>(this IQueryable<T> source, string keySelector)
        => source.OrderByDescending(ParseSelector<T, TKey>(keySelector));

    public static bool AnyDynamic<T>(this IQueryable<T> source, string predicate)
        => source.Any(ParsePredicate<T>(predicate));

    public static bool AllDynamic<T>(this IQueryable<T> source, string predicate)
        => source.All(ParsePredicate<T>(predicate));

    public static int CountDynamic<T>(this IQueryable<T> source, string predicate)
        => source.Count(ParsePredicate<T>(predicate));

    public static T FirstDynamic<T>(this IQueryable<T> source, string predicate)
        => source.First(ParsePredicate<T>(predicate));

    public static T? FirstOrDefaultDynamic<T>(this IQueryable<T> source, string predicate)
        => source.FirstOrDefault(ParsePredicate<T>(predicate));
}
