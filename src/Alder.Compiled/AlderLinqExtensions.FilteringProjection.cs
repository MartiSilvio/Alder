using System.Linq.Expressions;

namespace Alder.Compiled;

public static partial class AlderLinqExtensions
{
    public static IEnumerable<T> WhereDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.Where(CompilePredicate<T>(null, predicate, variables));

    public static IEnumerable<T> WhereDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Where(CompilePredicate<T>(engine, predicate, variables));

    public static IEnumerable<T> WhereDynamic<T>(this IEnumerable<T> source, Expression<Func<T, bool>> predicateExpr)
        => source.Where(CompilePredicate(predicateExpr));

    public static IEnumerable<T> WhereDynamic<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return source.Where(predicate);
    }

    public static IQueryable<T> WhereDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.Where(ParsePredicate<T>(null, predicate, variables));

    public static IQueryable<T> WhereDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.Where(ParsePredicate<T>(engine, predicate, variables));

    public static IQueryable<T> WhereDynamic<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicateExpr)
    {
        ArgumentNullException.ThrowIfNull(predicateExpr);
        return source.Where(predicateExpr);
    }

    public static IAsyncEnumerable<T> WhereDynamic<T>(
        this IAsyncEnumerable<T> source,
        string predicate,
        params object?[] variables)
        => source.WhereDynamic(GetGlobalEngine(), predicate, variables);

    public static async IAsyncEnumerable<T> WhereDynamic<T>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string predicate,
        params object?[] variables)
    {
        var compiled = CompilePredicate<T>(engine, predicate, variables);
        await foreach (var item in source)
            if (compiled(item))
                yield return item;
    }

    public static IAsyncEnumerable<T> WhereDynamic<T>(
        this IAsyncEnumerable<T> source,
        Expression<Func<T, bool>> predicateExpr)
        => source.WhereDynamic(CompilePredicate(predicateExpr));

    public static async IAsyncEnumerable<T> WhereDynamic<T>(
        this IAsyncEnumerable<T> source,
        Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        await foreach (var item in source)
            if (predicate(item))
                yield return item;
    }

    public static IEnumerable<TResult> SelectDynamic<T, TResult>(this IEnumerable<T> source, string selector, params object?[] variables)
        => source.Select(CompileSelector<T, TResult>(null, selector, variables));

    public static IEnumerable<TResult> SelectDynamic<T, TResult>(this IEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Select(CompileSelector<T, TResult>(engine, selector, variables));

    public static IEnumerable<TResult> SelectDynamic<T, TResult>(this IEnumerable<T> source, Expression<Func<T, TResult>> selectorExpr)
        => source.Select(CompileSelector(selectorExpr));

    public static IEnumerable<TResult> SelectDynamic<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return source.Select(selector);
    }

    public static IQueryable<TResult> SelectDynamic<T, TResult>(this IQueryable<T> source, string selector, params object?[] variables)
        => source.Select(ParseSelector<T, TResult>(null, selector, variables));

    public static IQueryable<TResult> SelectDynamic<T, TResult>(this IQueryable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.Select(ParseSelector<T, TResult>(engine, selector, variables));

    public static IQueryable<TResult> SelectDynamic<T, TResult>(this IQueryable<T> source, Expression<Func<T, TResult>> selectorExpr)
    {
        ArgumentNullException.ThrowIfNull(selectorExpr);
        return source.Select(selectorExpr);
    }

    public static IAsyncEnumerable<TResult> SelectDynamic<T, TResult>(
        this IAsyncEnumerable<T> source,
        string selector,
        params object?[] variables)
        => source.SelectDynamic<T, TResult>(GetGlobalEngine(), selector, variables);

    public static async IAsyncEnumerable<TResult> SelectDynamic<T, TResult>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string selector,
        params object?[] variables)
    {
        var compiled = CompileSelector<T, TResult>(engine, selector, variables);
        await foreach (var item in source)
            yield return compiled(item);
    }

    public static IAsyncEnumerable<TResult> SelectDynamic<T, TResult>(
        this IAsyncEnumerable<T> source,
        Expression<Func<T, TResult>> selectorExpr)
        => source.SelectDynamic(CompileSelector(selectorExpr));

    public static async IAsyncEnumerable<TResult> SelectDynamic<T, TResult>(
        this IAsyncEnumerable<T> source,
        Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        await foreach (var item in source)
            yield return selector(item);
    }
}
