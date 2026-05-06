var items = new[] { 10, 20, 30 };
var sum = 0;
foreach (var item in items)
{
    sum += await Task.FromResult(item);
}
return sum;
