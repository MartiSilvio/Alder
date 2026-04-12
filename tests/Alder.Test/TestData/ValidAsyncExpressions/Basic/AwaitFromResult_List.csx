// §15.15: await Task<List<int>> then aggregate with LINQ
var list = await Task.FromResult(new List<int> { 1, 2, 3, 4 });
return list.Sum();
