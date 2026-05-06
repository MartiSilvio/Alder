// §12.8.9.3: extension method — Distinct removes duplicates
var list = new List<int> { 1, 2, 2, 3, 3, 3 };
return list.Distinct().Count();
