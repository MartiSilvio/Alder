int[] arr = new[] { 10, 20, 30 };
int sum = 0;
foreach (var pair in Enumerable.Select(arr, (value, index) => new { value, index }))
{
    sum += pair.value * pair.index;
}
return sum;
