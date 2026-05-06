// Known limitation: the overload resolver does not select the indexed `Select<TSource, TResult>(
// this IEnumerable<TSource>, Func<TSource, int, TResult>)` overload when passed a two-parameter
// lambda — it picks the single-parameter overload and the lambda fails to bind.
int[] arr = new[] { 10, 20, 30 };
int sum = 0;
foreach (var pair in Enumerable.Select(arr, (value, index) => new { value, index }))
{
    sum += pair.value * pair.index;
}
return sum;
