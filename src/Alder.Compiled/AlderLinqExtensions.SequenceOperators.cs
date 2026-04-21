using System.Collections;
using System.Linq.Expressions;

namespace Alder.Compiled;

public static partial class AlderLinqExtensions
{
    public static IEnumerable<TElement> SelectManyDynamic<T, TElement>(this IEnumerable<T> source, string selector, params object?[] variables)
        => source.SelectMany(CompileCollectionSelector<T, TElement>(null, selector, variables));

    public static IEnumerable<TElement> SelectManyDynamic<T, TElement>(this IEnumerable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.SelectMany(CompileCollectionSelector<T, TElement>(engine, selector, variables));

    public static IEnumerable<TElement> SelectManyDynamic<T, TElement>(this IEnumerable<T> source, Expression<Func<T, IEnumerable<TElement>>> selectorExpr)
        => source.SelectMany(CompileSelector(selectorExpr));

    public static IEnumerable<TElement> SelectManyDynamic<T, TElement>(this IEnumerable<T> source, Func<T, IEnumerable<TElement>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return source.SelectMany(selector);
    }

    public static IQueryable<TElement> SelectManyDynamic<T, TElement>(this IQueryable<T> source, string selector, params object?[] variables)
        => source.SelectMany(ParseCollectionSelector<T, TElement>(null, selector, variables));

    public static IQueryable<TElement> SelectManyDynamic<T, TElement>(this IQueryable<T> source, AlderEngine engine, string selector, params object?[] variables)
        => source.SelectMany(ParseCollectionSelector<T, TElement>(engine, selector, variables));

    public static IQueryable<TElement> SelectManyDynamic<T, TElement>(this IQueryable<T> source, Expression<Func<T, IEnumerable<TElement>>> selectorExpr)
    {
        ArgumentNullException.ThrowIfNull(selectorExpr);
        return source.SelectMany(selectorExpr);
    }

    public static IEnumerable<TResult> SelectManyDynamic<T, TElement, TResult>(
        this IEnumerable<T> source,
        string collectionSelector,
        string resultSelector,
        params object?[] variables)
        => source.SelectMany(
            CompileCollectionSelector<T, TElement>(null, collectionSelector, variables),
            CompileBinaryLambda<T, TElement, TResult>(null, resultSelector, variables, "outer", "inner"));

    public static IEnumerable<TResult> SelectManyDynamic<T, TElement, TResult>(
        this IEnumerable<T> source,
        AlderEngine engine,
        string collectionSelector,
        string resultSelector,
        params object?[] variables)
        => source.SelectMany(
            CompileCollectionSelector<T, TElement>(engine, collectionSelector, variables),
            CompileBinaryLambda<T, TElement, TResult>(engine, resultSelector, variables, "outer", "inner"));

    public static IQueryable<TResult> SelectManyDynamic<T, TElement, TResult>(
        this IQueryable<T> source,
        string collectionSelector,
        string resultSelector,
        params object?[] variables)
        => source.SelectMany(
            ParseCollectionSelector<T, TElement>(null, collectionSelector, variables),
            ParseBinaryLambda<T, TElement, TResult>(null, resultSelector, variables, "outer", "inner"));

    public static IQueryable<TResult> SelectManyDynamic<T, TElement, TResult>(
        this IQueryable<T> source,
        AlderEngine engine,
        string collectionSelector,
        string resultSelector,
        params object?[] variables)
        => source.SelectMany(
            ParseCollectionSelector<T, TElement>(engine, collectionSelector, variables),
            ParseBinaryLambda<T, TElement, TResult>(engine, resultSelector, variables, "outer", "inner"));

    public static async IAsyncEnumerable<TElement> SelectManyDynamic<T, TElement>(
        this IAsyncEnumerable<T> source,
        string selector,
        params object?[] variables)
    {
        var compiled = CompileCollectionSelector<T, TElement>(null, selector, variables);
        await foreach (var item in source)
            foreach (var element in compiled(item))
                yield return element;
    }

    public static async IAsyncEnumerable<TElement> SelectManyDynamic<T, TElement>(
        this IAsyncEnumerable<T> source,
        AlderEngine engine,
        string selector,
        params object?[] variables)
    {
        var compiled = CompileCollectionSelector<T, TElement>(engine, selector, variables);
        await foreach (var item in source)
            foreach (var element in compiled(item))
                yield return element;
    }

    public static IOrderedEnumerable<T> OrderByDynamic<T, TKey>(this IEnumerable<T> source, string keySelector, params object?[] variables)
        => source.OrderBy(CompileSelector<T, TKey>(null, keySelector, variables));

    public static IOrderedEnumerable<T> OrderByDynamic<T, TKey>(this IEnumerable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.OrderBy(CompileSelector<T, TKey>(engine, keySelector, variables));

    public static IOrderedEnumerable<T> OrderByDynamic<T, TKey>(this IEnumerable<T> source, Expression<Func<T, TKey>> keySelectorExpr)
        => source.OrderBy(CompileSelector(keySelectorExpr));

    public static IOrderedEnumerable<T> OrderByDynamic<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return source.OrderBy(keySelector);
    }

    public static IOrderedEnumerable<T> OrderByDescendingDynamic<T, TKey>(this IEnumerable<T> source, string keySelector, params object?[] variables)
        => source.OrderByDescending(CompileSelector<T, TKey>(null, keySelector, variables));

    public static IOrderedEnumerable<T> OrderByDescendingDynamic<T, TKey>(this IEnumerable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.OrderByDescending(CompileSelector<T, TKey>(engine, keySelector, variables));

    public static IOrderedEnumerable<T> OrderByDescendingDynamic<T, TKey>(this IEnumerable<T> source, Expression<Func<T, TKey>> keySelectorExpr)
        => source.OrderByDescending(CompileSelector(keySelectorExpr));

    public static IOrderedEnumerable<T> OrderByDescendingDynamic<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return source.OrderByDescending(keySelector);
    }

    public static IOrderedEnumerable<T> ThenByDynamic<T, TKey>(this IOrderedEnumerable<T> source, string keySelector, params object?[] variables)
        => source.ThenBy(CompileSelector<T, TKey>(null, keySelector, variables));

    public static IOrderedEnumerable<T> ThenByDynamic<T, TKey>(this IOrderedEnumerable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.ThenBy(CompileSelector<T, TKey>(engine, keySelector, variables));

    public static IOrderedEnumerable<T> ThenByDynamic<T, TKey>(this IOrderedEnumerable<T> source, Expression<Func<T, TKey>> keySelectorExpr)
        => source.ThenBy(CompileSelector(keySelectorExpr));

    public static IOrderedEnumerable<T> ThenByDynamic<T, TKey>(this IOrderedEnumerable<T> source, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return source.ThenBy(keySelector);
    }

    public static IOrderedEnumerable<T> ThenByDescendingDynamic<T, TKey>(this IOrderedEnumerable<T> source, string keySelector, params object?[] variables)
        => source.ThenByDescending(CompileSelector<T, TKey>(null, keySelector, variables));

    public static IOrderedEnumerable<T> ThenByDescendingDynamic<T, TKey>(this IOrderedEnumerable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.ThenByDescending(CompileSelector<T, TKey>(engine, keySelector, variables));

    public static IOrderedEnumerable<T> ThenByDescendingDynamic<T, TKey>(this IOrderedEnumerable<T> source, Expression<Func<T, TKey>> keySelectorExpr)
        => source.ThenByDescending(CompileSelector(keySelectorExpr));

    public static IOrderedEnumerable<T> ThenByDescendingDynamic<T, TKey>(this IOrderedEnumerable<T> source, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return source.ThenByDescending(keySelector);
    }

    public static IOrderedQueryable<T> OrderByDynamic<T, TKey>(this IQueryable<T> source, string keySelector, params object?[] variables)
        => source.OrderBy(ParseSelector<T, TKey>(null, keySelector, variables));

    public static IOrderedQueryable<T> OrderByDynamic<T, TKey>(this IQueryable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.OrderBy(ParseSelector<T, TKey>(engine, keySelector, variables));

    public static IOrderedQueryable<T> OrderByDynamic<T, TKey>(this IQueryable<T> source, Expression<Func<T, TKey>> keySelectorExpr)
    {
        ArgumentNullException.ThrowIfNull(keySelectorExpr);
        return source.OrderBy(keySelectorExpr);
    }

    public static IOrderedQueryable<T> OrderByDescendingDynamic<T, TKey>(this IQueryable<T> source, string keySelector, params object?[] variables)
        => source.OrderByDescending(ParseSelector<T, TKey>(null, keySelector, variables));

    public static IOrderedQueryable<T> OrderByDescendingDynamic<T, TKey>(this IQueryable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.OrderByDescending(ParseSelector<T, TKey>(engine, keySelector, variables));

    public static IOrderedQueryable<T> OrderByDescendingDynamic<T, TKey>(this IQueryable<T> source, Expression<Func<T, TKey>> keySelectorExpr)
    {
        ArgumentNullException.ThrowIfNull(keySelectorExpr);
        return source.OrderByDescending(keySelectorExpr);
    }

    public static IOrderedQueryable<T> ThenByDynamic<T, TKey>(this IOrderedQueryable<T> source, string keySelector, params object?[] variables)
        => source.ThenBy(ParseSelector<T, TKey>(null, keySelector, variables));

    public static IOrderedQueryable<T> ThenByDynamic<T, TKey>(this IOrderedQueryable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.ThenBy(ParseSelector<T, TKey>(engine, keySelector, variables));

    public static IOrderedQueryable<T> ThenByDynamic<T, TKey>(this IOrderedQueryable<T> source, Expression<Func<T, TKey>> keySelectorExpr)
    {
        ArgumentNullException.ThrowIfNull(keySelectorExpr);
        return source.ThenBy(keySelectorExpr);
    }

    public static IOrderedQueryable<T> ThenByDescendingDynamic<T, TKey>(this IOrderedQueryable<T> source, string keySelector, params object?[] variables)
        => source.ThenByDescending(ParseSelector<T, TKey>(null, keySelector, variables));

    public static IOrderedQueryable<T> ThenByDescendingDynamic<T, TKey>(this IOrderedQueryable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.ThenByDescending(ParseSelector<T, TKey>(engine, keySelector, variables));

    public static IOrderedQueryable<T> ThenByDescendingDynamic<T, TKey>(this IOrderedQueryable<T> source, Expression<Func<T, TKey>> keySelectorExpr)
    {
        ArgumentNullException.ThrowIfNull(keySelectorExpr);
        return source.ThenByDescending(keySelectorExpr);
    }

    public static IEnumerable<T> SkipDynamic<T>(this IEnumerable<T> source, int count) => source.Skip(count);
    public static IQueryable<T> SkipDynamic<T>(this IQueryable<T> source, int count) => source.Skip(count);

    public static async IAsyncEnumerable<T> SkipDynamic<T>(this IAsyncEnumerable<T> source, int count)
    {
        var skipped = 0;
        await foreach (var item in source)
        {
            if (skipped++ < count)
                continue;
            yield return item;
        }
    }

    public static IEnumerable<T> TakeDynamic<T>(this IEnumerable<T> source, int count) => source.Take(count);
    public static IQueryable<T> TakeDynamic<T>(this IQueryable<T> source, int count) => source.Take(count);

    public static bool ContainsDynamic<T>(this IEnumerable<T> source, T value) => source.Contains(value);
    public static bool ContainsDynamic<T>(this IQueryable<T> source, T value) => source.Contains(value);

    public static T ElementAtDynamic<T>(this IEnumerable<T> source, int index) => source.ElementAt(index);
    public static T ElementAtDynamic<T>(this IQueryable<T> source, int index) => source.ElementAt(index);

    public static T? ElementAtOrDefaultDynamic<T>(this IEnumerable<T> source, int index) => source.ElementAtOrDefault(index);
    public static T? ElementAtOrDefaultDynamic<T>(this IQueryable<T> source, int index) => source.ElementAtOrDefault(index);

    public static async IAsyncEnumerable<T> TakeDynamic<T>(this IAsyncEnumerable<T> source, int count)
    {
        if (count <= 0)
            yield break;

        var taken = 0;
        await foreach (var item in source)
        {
            yield return item;
            taken++;
            if (taken >= count)
                yield break;
        }
    }

    public static IEnumerable<T> SkipWhileDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.SkipWhile(CompilePredicate<T>(null, predicate, variables));

    public static IEnumerable<T> SkipWhileDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.SkipWhile(CompilePredicate<T>(engine, predicate, variables));

    public static IQueryable<T> SkipWhileDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.SkipWhile(ParsePredicate<T>(null, predicate, variables));

    public static IQueryable<T> SkipWhileDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.SkipWhile(ParsePredicate<T>(engine, predicate, variables));

    public static async IAsyncEnumerable<T> SkipWhileDynamic<T>(this IAsyncEnumerable<T> source, string predicate, params object?[] variables)
    {
        var compiled = CompilePredicate<T>(null, predicate, variables);
        var skipping = true;
        await foreach (var item in source)
        {
            if (skipping && compiled(item))
                continue;

            skipping = false;
            yield return item;
        }
    }

    public static IEnumerable<T> TakeWhileDynamic<T>(this IEnumerable<T> source, string predicate, params object?[] variables)
        => source.TakeWhile(CompilePredicate<T>(null, predicate, variables));

    public static IEnumerable<T> TakeWhileDynamic<T>(this IEnumerable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.TakeWhile(CompilePredicate<T>(engine, predicate, variables));

    public static IQueryable<T> TakeWhileDynamic<T>(this IQueryable<T> source, string predicate, params object?[] variables)
        => source.TakeWhile(ParsePredicate<T>(null, predicate, variables));

    public static IQueryable<T> TakeWhileDynamic<T>(this IQueryable<T> source, AlderEngine engine, string predicate, params object?[] variables)
        => source.TakeWhile(ParsePredicate<T>(engine, predicate, variables));

    public static async IAsyncEnumerable<T> TakeWhileDynamic<T>(this IAsyncEnumerable<T> source, string predicate, params object?[] variables)
    {
        var compiled = CompilePredicate<T>(null, predicate, variables);
        await foreach (var item in source)
        {
            if (!compiled(item))
                yield break;

            yield return item;
        }
    }

    public static IEnumerable<IGrouping<TKey, T>> GroupByDynamic<T, TKey>(this IEnumerable<T> source, string keySelector, params object?[] variables)
        => source.GroupBy(CompileSelector<T, TKey>(null, keySelector, variables));

    public static IEnumerable<IGrouping<TKey, T>> GroupByDynamic<T, TKey>(this IEnumerable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.GroupBy(CompileSelector<T, TKey>(engine, keySelector, variables));

    public static IQueryable<IGrouping<TKey, T>> GroupByDynamic<T, TKey>(this IQueryable<T> source, string keySelector, params object?[] variables)
        => source.GroupBy(ParseSelector<T, TKey>(null, keySelector, variables));

    public static IQueryable<IGrouping<TKey, T>> GroupByDynamic<T, TKey>(this IQueryable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.GroupBy(ParseSelector<T, TKey>(engine, keySelector, variables));

    public static IEnumerable<TResult> JoinDynamic<TOuter, TInner, TKey, TResult>(
        this IEnumerable<TOuter> outer,
        IEnumerable<TInner> inner,
        string outerKeySelector,
        string innerKeySelector,
        string resultSelector,
        params object?[] variables)
        => outer.Join(
            inner,
            CompileSelector<TOuter, TKey>(null, outerKeySelector, variables),
            CompileSelector<TInner, TKey>(null, innerKeySelector, variables),
            CompileBinaryLambda<TOuter, TInner, TResult>(null, resultSelector, variables, "outer", "inner"));

    public static IEnumerable<TResult> JoinDynamic<TOuter, TInner, TKey, TResult>(
        this IEnumerable<TOuter> outer,
        IEnumerable<TInner> inner,
        AlderEngine engine,
        string outerKeySelector,
        string innerKeySelector,
        string resultSelector,
        params object?[] variables)
        => outer.Join(
            inner,
            CompileSelector<TOuter, TKey>(engine, outerKeySelector, variables),
            CompileSelector<TInner, TKey>(engine, innerKeySelector, variables),
            CompileBinaryLambda<TOuter, TInner, TResult>(engine, resultSelector, variables, "outer", "inner"));

    public static IQueryable<TResult> JoinDynamic<TOuter, TInner, TKey, TResult>(
        this IQueryable<TOuter> outer,
        IEnumerable<TInner> inner,
        string outerKeySelector,
        string innerKeySelector,
        string resultSelector,
        params object?[] variables)
        => outer.Join(
            inner.AsQueryable(),
            ParseSelector<TOuter, TKey>(null, outerKeySelector, variables),
            ParseSelector<TInner, TKey>(null, innerKeySelector, variables),
            ParseBinaryLambda<TOuter, TInner, TResult>(null, resultSelector, variables, "outer", "inner"));

    public static IQueryable<TResult> JoinDynamic<TOuter, TInner, TKey, TResult>(
        this IQueryable<TOuter> outer,
        IEnumerable<TInner> inner,
        AlderEngine engine,
        string outerKeySelector,
        string innerKeySelector,
        string resultSelector,
        params object?[] variables)
        => outer.Join(
            inner.AsQueryable(),
            ParseSelector<TOuter, TKey>(engine, outerKeySelector, variables),
            ParseSelector<TInner, TKey>(engine, innerKeySelector, variables),
            ParseBinaryLambda<TOuter, TInner, TResult>(engine, resultSelector, variables, "outer", "inner"));

    public static IEnumerable<TResult> GroupJoinDynamic<TOuter, TInner, TKey, TResult>(
        this IEnumerable<TOuter> outer,
        IEnumerable<TInner> inner,
        string outerKeySelector,
        string innerKeySelector,
        string resultSelector,
        params object?[] variables)
        => outer.GroupJoin(
            inner,
            CompileSelector<TOuter, TKey>(null, outerKeySelector, variables),
            CompileSelector<TInner, TKey>(null, innerKeySelector, variables),
            CompileBinaryLambda<TOuter, IEnumerable<TInner>, TResult>(null, resultSelector, variables, "outer", "group"));

    public static IEnumerable<TResult> GroupJoinDynamic<TOuter, TInner, TKey, TResult>(
        this IEnumerable<TOuter> outer,
        IEnumerable<TInner> inner,
        AlderEngine engine,
        string outerKeySelector,
        string innerKeySelector,
        string resultSelector,
        params object?[] variables)
        => outer.GroupJoin(
            inner,
            CompileSelector<TOuter, TKey>(engine, outerKeySelector, variables),
            CompileSelector<TInner, TKey>(engine, innerKeySelector, variables),
            CompileBinaryLambda<TOuter, IEnumerable<TInner>, TResult>(engine, resultSelector, variables, "outer", "group"));

    public static IQueryable<TResult> GroupJoinDynamic<TOuter, TInner, TKey, TResult>(
        this IQueryable<TOuter> outer,
        IEnumerable<TInner> inner,
        string outerKeySelector,
        string innerKeySelector,
        string resultSelector,
        params object?[] variables)
        => outer.GroupJoin(
            inner.AsQueryable(),
            ParseSelector<TOuter, TKey>(null, outerKeySelector, variables),
            ParseSelector<TInner, TKey>(null, innerKeySelector, variables),
            ParseBinaryLambda<TOuter, IEnumerable<TInner>, TResult>(null, resultSelector, variables, "outer", "group"));

    public static IQueryable<TResult> GroupJoinDynamic<TOuter, TInner, TKey, TResult>(
        this IQueryable<TOuter> outer,
        IEnumerable<TInner> inner,
        AlderEngine engine,
        string outerKeySelector,
        string innerKeySelector,
        string resultSelector,
        params object?[] variables)
        => outer.GroupJoin(
            inner.AsQueryable(),
            ParseSelector<TOuter, TKey>(engine, outerKeySelector, variables),
            ParseSelector<TInner, TKey>(engine, innerKeySelector, variables),
            ParseBinaryLambda<TOuter, IEnumerable<TInner>, TResult>(engine, resultSelector, variables, "outer", "group"));

    public static IEnumerable<T> DistinctDynamic<T>(this IEnumerable<T> source) => source.Distinct();
    public static IQueryable<T> DistinctDynamic<T>(this IQueryable<T> source) => source.Distinct();

    public static async IAsyncEnumerable<T> DistinctDynamic<T>(this IAsyncEnumerable<T> source)
    {
        var seen = new HashSet<T>();
        await foreach (var item in source)
            if (seen.Add(item!))
                yield return item;
    }

    public static IEnumerable<T> DistinctByDynamic<T, TKey>(this IEnumerable<T> source, string keySelector, params object?[] variables)
        => source.DistinctBy(CompileSelector<T, TKey>(null, keySelector, variables));

    public static IEnumerable<T> DistinctByDynamic<T, TKey>(this IEnumerable<T> source, AlderEngine engine, string keySelector, params object?[] variables)
        => source.DistinctBy(CompileSelector<T, TKey>(engine, keySelector, variables));

    public static IEnumerable<T> ConcatDynamic<T>(this IEnumerable<T> first, IEnumerable<T> second) =>
        first.Concat(second);

    public static IQueryable<T> ConcatDynamic<T>(this IQueryable<T> first, IQueryable<T> second) =>
        first.Concat(second);

    public static IEnumerable<T> UnionDynamic<T>(this IEnumerable<T> first, IEnumerable<T> second) =>
        first.Union(second);

    public static IQueryable<T> UnionDynamic<T>(this IQueryable<T> first, IQueryable<T> second) =>
        first.Union(second);

    public static IEnumerable<T> IntersectDynamic<T>(this IEnumerable<T> first, IEnumerable<T> second) =>
        first.Intersect(second);

    public static IQueryable<T> IntersectDynamic<T>(this IQueryable<T> first, IQueryable<T> second) =>
        first.Intersect(second);

    public static IEnumerable<T> ExceptDynamic<T>(this IEnumerable<T> first, IEnumerable<T> second) =>
        first.Except(second);

    public static IQueryable<T> ExceptDynamic<T>(this IQueryable<T> first, IQueryable<T> second) =>
        first.Except(second);

    public static IEnumerable<T> DefaultIfEmptyDynamic<T>(this IEnumerable<T> source) =>
        source.DefaultIfEmpty()!;

    public static IQueryable<T> DefaultIfEmptyDynamic<T>(this IQueryable<T> source) =>
        source.DefaultIfEmpty()!;

    public static IEnumerable<T> DefaultIfEmptyDynamic<T>(this IEnumerable<T> source, T defaultValue) =>
        source.DefaultIfEmpty(defaultValue);

    public static IQueryable<T> DefaultIfEmptyDynamic<T>(this IQueryable<T> source, T defaultValue) =>
        source.DefaultIfEmpty(defaultValue);

    public static IEnumerable<TResult> OfTypeDynamic<TResult>(this IEnumerable source) =>
        source.OfType<TResult>();

    public static IQueryable<TResult> OfTypeDynamic<TResult>(this IQueryable source) =>
        source.OfType<TResult>();

    public static IEnumerable<TResult> CastDynamic<TResult>(this IEnumerable source) =>
        source.Cast<TResult>();

    public static IQueryable<TResult> CastDynamic<TResult>(this IQueryable source) =>
        source.Cast<TResult>();

    public static bool SequenceEqualDynamic<T>(this IEnumerable<T> first, IEnumerable<T> second) =>
        first.SequenceEqual(second);

    public static bool SequenceEqualDynamic<T>(this IQueryable<T> first, IQueryable<T> second) =>
        first.SequenceEqual(second);

    public static IEnumerable<T> AppendDynamic<T>(this IEnumerable<T> source, T element) =>
        source.Append(element);

    public static IQueryable<T> AppendDynamic<T>(this IQueryable<T> source, T element) =>
        source.Append(element);

    public static IEnumerable<T> PrependDynamic<T>(this IEnumerable<T> source, T element) =>
        source.Prepend(element);

    public static IQueryable<T> PrependDynamic<T>(this IQueryable<T> source, T element) =>
        source.Prepend(element);

    public static IEnumerable<T> ReverseDynamic<T>(this IEnumerable<T> source) => source.Reverse();
    public static IQueryable<T> ReverseDynamic<T>(this IQueryable<T> source) => source.Reverse();

    public static async IAsyncEnumerable<T> ReverseDynamic<T>(this IAsyncEnumerable<T> source)
    {
        var items = new List<T>();
        await foreach (var item in source)
            items.Add(item);

        for (var i = items.Count - 1; i >= 0; i--)
            yield return items[i];
    }
}
