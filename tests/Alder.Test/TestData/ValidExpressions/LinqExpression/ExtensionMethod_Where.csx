// §12.8.9.3: extension method invocation — LINQ Where
var list = new List<int> { 1, 2, 3, 4, 5 };
return list.Where(x => x > 3).Count();
