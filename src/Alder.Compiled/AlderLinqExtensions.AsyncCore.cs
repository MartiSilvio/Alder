using System.Collections;
using Alder.Compiled.DynamicLinq;

namespace Alder.Compiled;

public static partial class AlderLinqExtensions
{
    private static async IAsyncEnumerable<T> AsyncWhereCore<T>(IAsyncEnumerable<T> source, Func<T, bool> predicate)
    {
        await foreach (var item in source)
            if (predicate(item))
                yield return item;
    }

    private static async IAsyncEnumerable<TResult> AsyncSelectCore<T, TResult>(IAsyncEnumerable<T> source, Func<T, TResult> selector)
    {
        await foreach (var item in source)
            yield return selector(item);
    }

    private static async IAsyncEnumerable<TElement> AsyncSelectManyCore<T, TElement>(
        IAsyncEnumerable<T> source,
        Func<T, IEnumerable<TElement>> selector)
    {
        await foreach (var item in source)
            foreach (var element in selector(item))
                yield return element;
    }

    private static IAsyncEnumerable<object?> AsyncSelectBoxedCore<T>(
        IAsyncEnumerable<T> source,
        AlderEngine engine,
        string selector,
        IReadOnlyList<KeyValuePair<string, object?>>? variables)
        => AsyncSelectCore(source, CompileBoxedSelector<T>(engine, selector, variables, Compilation.DynamicQueryLambdaKind.Selector));

    private static IAsyncEnumerable<object?> AsyncSelectManyBoxedCore<T>(
        IAsyncEnumerable<T> source,
        AlderEngine engine,
        string selector,
        IReadOnlyList<KeyValuePair<string, object?>>? variables)
        => AsyncSelectManyUntypedCore(source, CompileUntypedCollectionSelector<T>(engine, selector, variables));

    private static async IAsyncEnumerable<object?> AsyncSelectManyUntypedCore<T>(
        IAsyncEnumerable<T> source,
        Func<T, IEnumerable> selector)
    {
        await foreach (var item in source)
            foreach (var element in selector(item))
                yield return element;
    }

    private static async IAsyncEnumerable<T> AsyncSkipCore<T>(IAsyncEnumerable<T> source, int count)
    {
        var skipped = 0;
        await foreach (var item in source)
        {
            if (skipped++ < count)
                continue;
            yield return item;
        }
    }

    private static async IAsyncEnumerable<T> AsyncTakeCore<T>(IAsyncEnumerable<T> source, int count)
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

    private static async IAsyncEnumerable<T> AsyncSkipWhileCore<T>(IAsyncEnumerable<T> source, Func<T, bool> predicate)
    {
        var skipping = true;
        await foreach (var item in source)
        {
            if (skipping && predicate(item))
                continue;

            skipping = false;
            yield return item;
        }
    }

    private static async IAsyncEnumerable<T> AsyncTakeWhileCore<T>(IAsyncEnumerable<T> source, Func<T, bool> predicate)
    {
        await foreach (var item in source)
        {
            if (!predicate(item))
                yield break;

            yield return item;
        }
    }

    private static async IAsyncEnumerable<T> AsyncDistinctCore<T>(IAsyncEnumerable<T> source)
    {
        foreach (var item in Enumerable.Distinct(await ToListAsync(source)))
            yield return item;
    }

    private static async IAsyncEnumerable<T> AsyncReverseCore<T>(IAsyncEnumerable<T> source)
    {
        foreach (var item in Enumerable.Reverse(await ToListAsync(source)))
            yield return item;
    }

    private static async ValueTask<bool> AsyncAnyCore<T>(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        => Enumerable.Any(await ToListAsync(source), predicate);

    private static async ValueTask<bool> AsyncAllCore<T>(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        => Enumerable.All(await ToListAsync(source), predicate);

    private static async ValueTask<int> AsyncCountCore<T>(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        => Enumerable.Count(await ToListAsync(source), predicate);

    private static async ValueTask<long> AsyncLongCountCore<T>(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        => Enumerable.LongCount(await ToListAsync(source), predicate);

    private static async ValueTask<decimal> AsyncSumDecimalCore<T>(IAsyncEnumerable<T> source, Func<T, decimal> selector)
        => Enumerable.Sum(Enumerable.Select(await ToListAsync(source), selector));

    private static async ValueTask<T> AsyncFirstCore<T>(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        => Enumerable.First(await ToListAsync(source), predicate);

    private static async ValueTask<T?> AsyncFirstOrDefaultCore<T>(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        => Enumerable.FirstOrDefault(await ToListAsync(source), predicate);

    private static async ValueTask<T> AsyncLastCore<T>(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        => Enumerable.Last(await ToListAsync(source), predicate);

    private static async ValueTask<T?> AsyncLastOrDefaultCore<T>(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        => Enumerable.LastOrDefault(await ToListAsync(source), predicate);

    private static async ValueTask<T> AsyncSingleCore<T>(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        => Enumerable.Single(await ToListAsync(source), predicate);

    private static async ValueTask<T?> AsyncSingleOrDefaultCore<T>(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        => Enumerable.SingleOrDefault(await ToListAsync(source), predicate);

    private static async ValueTask<object> AsyncSumObjectCore<T>(
        IAsyncEnumerable<T> source,
        AlderEngine engine,
        string selector,
        IReadOnlyList<KeyValuePair<string, object?>>? variables)
        => DynamicQueryDispatcher.Sum(await ToListAsync(source), engine, selector, variables);

    private static async ValueTask<object> AsyncAverageObjectCore<T>(
        IAsyncEnumerable<T> source,
        AlderEngine engine,
        string selector,
        IReadOnlyList<KeyValuePair<string, object?>>? variables)
        => DynamicQueryDispatcher.Average(await ToListAsync(source), engine, selector, variables);

    private static async ValueTask<object> AsyncMinObjectCore<T>(
        IAsyncEnumerable<T> source,
        AlderEngine engine,
        string selector,
        IReadOnlyList<KeyValuePair<string, object?>>? variables)
        => DynamicQueryDispatcher.Min(await ToListAsync(source), engine, selector, variables);

    private static async ValueTask<object> AsyncMaxObjectCore<T>(
        IAsyncEnumerable<T> source,
        AlderEngine engine,
        string selector,
        IReadOnlyList<KeyValuePair<string, object?>>? variables)
        => DynamicQueryDispatcher.Max(await ToListAsync(source), engine, selector, variables);

    private static async ValueTask<TResult> AsyncConvertScalarCore<TResult>(ValueTask<object> value)
        => (TResult)await value;

    private static async ValueTask<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
            list.Add(item);
        return list;
    }
}
