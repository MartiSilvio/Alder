// §12.8.9.3: extension method — OrderByDescending
var list = new List<int> { 3, 1, 4, 1, 5 };
return list.OrderByDescending(x => x).First();
