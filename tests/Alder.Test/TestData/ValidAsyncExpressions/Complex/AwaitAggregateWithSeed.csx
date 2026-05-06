// §15.15: aggregate using LINQ on an awaited list
var list = await Task.FromResult(new List<int> { 1, 2, 3, 4 });
return list.Aggregate(0, (acc, x) => acc + x);
