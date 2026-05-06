// §15.15: foreach over pre-awaited collection, inner await per element
var items = await Task.FromResult(new[] { 1, 2, 3, 4, 5 });
int total = 0;
foreach (var x in items)
{
    total += await Task.FromResult(x);
}
return total;
