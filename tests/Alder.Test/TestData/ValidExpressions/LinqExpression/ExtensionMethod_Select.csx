// §12.8.9.3: extension method invocation — LINQ Select
var list = new List<int> { 1, 2, 3 };
return list.Select(x => x * 10).ToList()[1];
