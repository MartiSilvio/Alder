// §12.8.9.3: extension method — Aggregate (fold)
var list = new List<int> { 1, 2, 3, 4 };
return list.Aggregate(0, (acc, x) => acc + x);
