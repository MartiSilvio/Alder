// §15.15: awaited list processed with LINQ Select
var items = await Task.FromResult(new List<int> { 1, 2, 3 });
return items.Select(x => x * x).Sum();
