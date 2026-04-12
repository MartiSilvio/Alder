// §12.8.9.3: extension method — All with predicate
var list = new List<int> { 2, 4, 6 };
return list.All(x => x % 2 == 0);
